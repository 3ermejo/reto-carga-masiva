using Control.Api.Application.Common;
using Control.Api.Application.Dtos;
using Control.Api.Application.Interfaces;
using Control.Api.Application.Validation;
using Control.Api.Domain;
using Shared.Messaging.Contracts;
using Shared.Messaging.RabbitMq;

namespace Control.Api.Application.Services;

public class CargaService : ICargaService
{
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const int MaxCargasEnHistorial = 200;

    private readonly IExcelFileValidator _fileValidator;
    private readonly IFileStorage _fileStorage;
    private readonly ICargaRepository _repository;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<CargaService> _logger;

    public CargaService(
        IExcelFileValidator fileValidator,
        IFileStorage fileStorage,
        ICargaRepository repository,
        IMessagePublisher publisher,
        ILogger<CargaService> logger)
    {
        _fileValidator = fileValidator;
        _fileStorage = fileStorage;
        _repository = repository;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// 1. Valida el archivo  2. Registra la carga (Pendiente)  3. Sube el archivo a SeaweedFS
    /// 4. Publica el mensaje en la cola carga_masiva. El procesamiento es asincrono.
    /// </summary>
    public async Task<OperationResult<CargaAceptadaResponse>> RegistrarAsync(IFormFile? archivo, CurrentUser usuario, CancellationToken cancellationToken)
    {
        if (archivo is null)
            return Fail("Debe adjuntar un archivo Excel (.xlsx).", StatusCodes.Status400BadRequest);

        await using var contenido = archivo.OpenReadStream();

        var validacion = _fileValidator.Validate(archivo.FileName, archivo.Length, contenido);
        if (!validacion.IsValid)
            return Fail(validacion.ErrorMessage!, StatusCodes.Status400BadRequest);

        // El nombre en el almacenamiento lo genera el servidor (nunca se usa el del usuario)
        var ahora = DateTime.UtcNow;
        var rutaRelativa = $"uploads/{ahora:yyyy}/{ahora:MM}/{Guid.NewGuid():N}.xlsx";
        var nombreArchivo = FileNameSanitizer.Sanitize(archivo.FileName);

        var idCarga = await _repository.RegistrarAsync(
            nombreArchivo, usuario.Email, usuario.Id, $"seaweed://{rutaRelativa}", archivo.Length, cancellationToken);

        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["IdCarga"] = idCarga });
        _logger.LogInformation("Carga registrada en estado Pendiente por el usuario {UserId}", usuario.Id);

        string rutaArchivo;
        try
        {
            rutaArchivo = await _fileStorage.UploadAsync(rutaRelativa, contenido, ExcelContentType, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "No se pudo guardar el archivo en SeaweedFS");
            await _repository.FinalizarConErrorAsync(idCarga, "No se pudo almacenar el archivo en SeaweedFS.", cancellationToken);
            return Fail("El servicio de almacenamiento no está disponible. Intente nuevamente.", StatusCodes.Status503ServiceUnavailable);
        }

        try
        {
            await _publisher.PublishAsync(
                new CargaRegistradaMessage(idCarga, rutaArchivo, usuario.Email),
                RabbitMqTopology.CargaRegistradaRoutingKey,
                correlationId: idCarga.ToString(),
                cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "No se pudo publicar el mensaje en la cola carga_masiva");
            await _repository.FinalizarConErrorAsync(idCarga, "No se pudo encolar el archivo para su procesamiento.", cancellationToken);
            return Fail("El servicio de colas no está disponible. Intente nuevamente.", StatusCodes.Status503ServiceUnavailable);
        }

        return OperationResult<CargaAceptadaResponse>.Success(new CargaAceptadaResponse(idCarga, "Pendiente"));
    }

    public Task<IReadOnlyList<CargaResumen>> ListarAsync(CurrentUser usuario, CancellationToken cancellationToken)
    {
        var usuarioId = usuario.PuedeVerTodas ? (int?)null : usuario.Id;
        return _repository.ListarAsync(usuarioId, MaxCargasEnHistorial, cancellationToken);
    }

    public async Task<CargaDetalleResponse?> ObtenerDetalleAsync(int idCarga, CurrentUser usuario, CancellationToken cancellationToken)
    {
        var carga = await ObtenerSiTieneAccesoAsync(idCarga, usuario, cancellationToken);
        if (carga is null)
            return null;

        var historial = await _repository.ListarHistorialAsync(idCarga, cancellationToken);
        return CargaDetalleResponse.From(carga, historial);
    }

    public async Task<PagedResult<RegistroProcesado>?> ListarRegistrosAsync(int idCarga, int pagina, int tamano, CurrentUser usuario, CancellationToken cancellationToken)
    {
        if (await ObtenerSiTieneAccesoAsync(idCarga, usuario, cancellationToken) is null)
            return null;

        var registros = await _repository.ListarRegistrosAsync(idCarga, pagina, tamano, cancellationToken);
        return new PagedResult<RegistroProcesado>(registros, pagina, tamano, registros.FirstOrDefault()?.TotalFilas ?? 0);
    }

    public async Task<PagedResult<RegistroFallido>?> ListarFallidosAsync(int idCarga, int pagina, int tamano, CurrentUser usuario, CancellationToken cancellationToken)
    {
        if (await ObtenerSiTieneAccesoAsync(idCarga, usuario, cancellationToken) is null)
            return null;

        var fallidos = await _repository.ListarFallidosAsync(idCarga, pagina, tamano, cancellationToken);
        return new PagedResult<RegistroFallido>(fallidos, pagina, tamano, fallidos.FirstOrDefault()?.TotalFilas ?? 0);
    }

    public async Task<ArchivoDescarga?> DescargarArchivoAsync(int idCarga, CurrentUser usuario, CancellationToken cancellationToken)
    {
        var carga = await ObtenerSiTieneAccesoAsync(idCarga, usuario, cancellationToken);
        if (carga is null)
            return null;

        var contenido = await _fileStorage.OpenReadAsync(carga.RutaArchivo, cancellationToken);
        return new ArchivoDescarga(contenido, carga.NombreArchivo, ExcelContentType);
    }

    /// <summary>Evita IDOR: si la carga es de otro usuario se responde igual que si no existiera.</summary>
    private async Task<Carga?> ObtenerSiTieneAccesoAsync(int idCarga, CurrentUser usuario, CancellationToken cancellationToken)
    {
        var carga = await _repository.ObtenerAsync(idCarga, cancellationToken);
        return carga is not null && usuario.PuedeVer(carga) ? carga : null;
    }

    private static OperationResult<CargaAceptadaResponse> Fail(string error, int statusCode) =>
        OperationResult<CargaAceptadaResponse>.Failure(error, statusCode);
}
