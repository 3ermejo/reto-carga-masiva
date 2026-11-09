using BulkLoad.Worker.Application.Exceptions;
using BulkLoad.Worker.Application.Interfaces;
using BulkLoad.Worker.Application.Validation;
using BulkLoad.Worker.Domain;
using Shared.Messaging.Contracts;
using Shared.Messaging.RabbitMq;

namespace BulkLoad.Worker.Application.Services;

/// <summary>
/// Procesa una carga:
///  1. Pendiente -> EnProceso
///  2. Descarga el Excel de SeaweedFS y lo lee (las filas vacias se omiten)
///  3. Limpia y valida cada fila (DatoInvalido) y detecta codigos repetidos en el archivo (Existente)
///  4. Valida la duplicidad por periodo (si falla, la carga se rechaza completa)
///  5. Inserta en DataProcesada; los codigos que ya existian se reportan como Existente
///  6. Cargado -> guarda los fallidos -> Finalizado
///  7. Publica el mensaje en la cola notificaciones
/// </summary>
public class CargaProcessor : ICargaProcessor
{
    private readonly ICargaRepository _repository;
    private readonly IFileStorage _fileStorage;
    private readonly IExcelReader _excelReader;
    private readonly RegistroValidator _validator;
    private readonly IMessagePublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CargaProcessor> _logger;

    public CargaProcessor(
        ICargaRepository repository,
        IFileStorage fileStorage,
        IExcelReader excelReader,
        RegistroValidator validator,
        IMessagePublisher publisher,
        TimeProvider timeProvider,
        ILogger<CargaProcessor> logger)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _excelReader = excelReader;
        _validator = validator;
        _publisher = publisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task ProcesarAsync(CargaRegistradaMessage mensaje, CancellationToken cancellationToken)
    {
        var inicio = await _repository.IniciarProcesoAsync(mensaje.IdCarga, cancellationToken);

        if (inicio == InicioProceso.NoExiste)
        {
            _logger.LogWarning("La carga no existe; se descarta el mensaje");
            return;
        }

        // Idempotencia: RabbitMQ puede entregar el mismo mensaje mas de una vez
        if (inicio == InicioProceso.Omitir)
        {
            _logger.LogInformation("La carga ya fue procesada y notificada; se ignora el mensaje repetido");
            return;
        }

        // La carga se finalizo pero fallo la publicacion de la notificacion: solo se vuelve a publicar
        if (inicio == InicioProceso.ReenviarNotificacion)
        {
            _logger.LogWarning("La carga ya estaba finalizada pero sin notificar; se vuelve a publicar la notificación");
            await PublicarNotificacionAsync(mensaje, cancellationToken);
            return;
        }

        IReadOnlyList<FilaExcel> filas;
        try
        {
            await using var archivo = await _fileStorage.DownloadAsync(mensaje.RutaArchivo, cancellationToken);
            filas = _excelReader.Leer(archivo);
        }
        catch (Exception ex) when (ex is InvalidExcelException or FileNotFoundException)
        {
            // Errores del archivo: reintentar no los arregla
            _logger.LogWarning("El archivo no se pudo procesar: {Motivo}", ex.Message);
            await FinalizarYNotificarAsync(mensaje, 0, 0, 0, ResultadoCarga.Rechazado, ex.Message, cancellationToken);
            return;
        }

        var (validos, fallidos) = ValidarFilas(filas);

        if (filas.Count == 0)
        {
            await FinalizarYNotificarAsync(mensaje, 0, 0, 0, ResultadoCarga.Rechazado, "El archivo no contiene registros.", cancellationToken);
            return;
        }

        if (validos.Count == 0)
        {
            await _repository.RegistrarFallidosAsync(mensaje.IdCarga, fallidos, cancellationToken);
            await FinalizarYNotificarAsync(mensaje, filas.Count, 0, fallidos.Count, ResultadoCarga.Rechazado,
                "Ninguna fila del archivo es válida.", cancellationToken);
            return;
        }

        // Validacion de duplicidad por periodo
        var periodos = validos.Select(r => r.Periodo).Distinct().Order().ToList();
        var validacion = await _repository.ValidarPeriodosAsync(mensaje.IdCarga, periodos, cancellationToken);

        if (!validacion.EsValido)
        {
            var motivo = validacion.Resultado == ValidacionPeriodo.Rechazado
                ? $"El periodo {validacion.Periodo} ya fue cargado (carga #{validacion.CargaConflicto}, estado {validacion.EstadoConflicto})."
                : $"El periodo {validacion.Periodo} tiene otra carga en curso (carga #{validacion.CargaConflicto}, estado {validacion.EstadoConflicto}).";

            _logger.LogWarning("Carga rechazada por periodo: {Motivo}", motivo);

            fallidos.AddRange(validos.Select(r => new RegistroFallido(
                r.Fila, r.CodigoProducto, MotivoFallo.PeriodoRechazado, motivo, DatosDe(r))));

            await _repository.RegistrarFallidosAsync(mensaje.IdCarga, fallidos, cancellationToken);
            await FinalizarYNotificarAsync(mensaje, filas.Count, 0, fallidos.Count, ResultadoCarga.Rechazado, motivo, cancellationToken);
            return;
        }

        // Insercion: los codigos que ya existen en la BD se reportan como Existente
        var resultados = await _repository.InsertarRegistrosAsync(mensaje.IdCarga, validos, cancellationToken);
        var porFila = validos.ToDictionary(r => r.Fila);
        var insertados = 0;

        foreach (var resultado in resultados)
        {
            if (resultado.Insertado)
            {
                insertados++;
                continue;
            }

            var registro = porFila[resultado.Fila];
            fallidos.Add(new RegistroFallido(
                registro.Fila, registro.CodigoProducto, MotivoFallo.Existente,
                $"El CodigoProducto {registro.CodigoProducto} ya existe en la base de datos.", DatosDe(registro)));
        }

        await _repository.MarcarCargadoAsync(mensaje.IdCarga, insertados, cancellationToken);
        await _repository.RegistrarFallidosAsync(mensaje.IdCarga, fallidos, cancellationToken);

        var resultadoCarga = fallidos.Count == 0 ? ResultadoCarga.Exitoso : ResultadoCarga.ConObservaciones;
        var resumen = Resumen(insertados, fallidos);

        _logger.LogInformation(
            "Carga procesada. Filas: {Total}, insertadas: {Insertados}, fallidas: {Fallidos}",
            filas.Count, insertados, fallidos.Count);

        await FinalizarYNotificarAsync(mensaje, filas.Count, insertados, fallidos.Count, resultadoCarga, resumen, cancellationToken);
    }

    /// <summary>Se usa cuando fallaron todos los reintentos (por ejemplo, la BD no responde).</summary>
    public async Task FinalizarConErrorAsync(CargaRegistradaMessage mensaje, string motivo, CancellationToken cancellationToken)
    {
        try
        {
            await FinalizarYNotificarAsync(mensaje, 0, 0, 0, ResultadoCarga.Error, motivo, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo registrar el error de la carga");
        }
    }

    private (List<RegistroProducto> Validos, List<RegistroFallido> Fallidos) ValidarFilas(IReadOnlyList<FilaExcel> filas)
    {
        var periodoPorDefecto = _timeProvider.GetUtcNow().ToString("yyyy-MM");
        var validos = new List<RegistroProducto>();
        var fallidos = new List<RegistroFallido>();
        var primeraFilaPorCodigo = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var fila in filas)
        {
            var validacion = _validator.Validar(fila, periodoPorDefecto);

            if (!validacion.EsValido)
            {
                fallidos.Add(new RegistroFallido(
                    fila.Fila, FilaExcel.ToText(fila.CodigoProducto), MotivoFallo.DatoInvalido, validacion.Error!, fila.ADiccionario()));
                continue;
            }

            var registro = validacion.Registro!;

            // Duplicado dentro del mismo archivo: se registra la primera aparicion
            if (primeraFilaPorCodigo.TryGetValue(registro.CodigoProducto, out var primeraFila))
            {
                fallidos.Add(new RegistroFallido(
                    fila.Fila, registro.CodigoProducto, MotivoFallo.Existente,
                    $"El CodigoProducto {registro.CodigoProducto} ya está en la fila {primeraFila} del archivo.", fila.ADiccionario()));
                continue;
            }

            primeraFilaPorCodigo[registro.CodigoProducto] = fila.Fila;
            validos.Add(registro);
        }

        return (validos, fallidos);
    }

    private async Task FinalizarYNotificarAsync(
        CargaRegistradaMessage mensaje, int total, int insertados, int fallidos,
        string resultado, string? detalle, CancellationToken cancellationToken)
    {
        await _repository.FinalizarAsync(mensaje.IdCarga, total, insertados, fallidos, resultado, detalle, cancellationToken);
        await PublicarNotificacionAsync(mensaje, cancellationToken);
    }

    private Task PublicarNotificacionAsync(CargaRegistradaMessage mensaje, CancellationToken cancellationToken)
    {
        return _publisher.PublishAsync(
            new CargaFinalizadaMessage(mensaje.IdCarga, mensaje.Usuario, _timeProvider.GetUtcNow().UtcDateTime),
            RabbitMqTopology.CargaFinalizadaRoutingKey,
            correlationId: mensaje.IdCarga.ToString(),
            cancellationToken);
    }

    private static string Resumen(int insertados, List<RegistroFallido> fallidos)
    {
        var existentes = fallidos.Count(f => f.Motivo == MotivoFallo.Existente);
        var invalidos = fallidos.Count(f => f.Motivo == MotivoFallo.DatoInvalido);
        return $"{insertados} registros insertados, {existentes} existentes, {invalidos} con datos inválidos.";
    }

    private static IReadOnlyDictionary<string, string?> DatosDe(RegistroProducto registro) => new Dictionary<string, string?>
    {
        ["Periodo"] = registro.Periodo,
        ["CodigoProducto"] = registro.CodigoProducto,
        ["NombreProducto"] = registro.NombreProducto,
        ["Precio"] = registro.Precio.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };
}
