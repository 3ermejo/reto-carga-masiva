using System.Text;
using BulkLoad.Worker.Application.Interfaces;
using BulkLoad.Worker.Application.Services;
using BulkLoad.Worker.Application.Validation;
using BulkLoad.Worker.Domain;
using BulkLoad.Worker.Infrastructure.Excel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shared.Messaging.Contracts;
using Shared.Messaging.RabbitMq;

namespace BulkLoad.Worker.Tests;

public class CargaProcessorTests
{
    private readonly FakeRepository _repository = new();
    private readonly FakePublisher _publisher = new();

    public CargaProcessorTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public async Task Procesar_ArchivoDelReto_InsertaLosUnicosYReportaLosRepetidosComoExistentes()
    {
        var processor = CreateProcessor("carga_masiva_productos.xlsx");

        await processor.ProcesarAsync(new CargaRegistradaMessage(1, "seaweed://uploads/a.xlsx", "admin@test.com"), CancellationToken.None);

        // El archivo trae 200 filas con 116 codigos distintos
        Assert.Equal(116, _repository.Insertados.Count);
        Assert.Equal(84, _repository.Fallidos.Count(f => f.Motivo == MotivoFallo.Existente));
        Assert.Equal(ResultadoCarga.ConObservaciones, _repository.ResultadoFinal);
        Assert.True(_repository.MarcadoCargado);
        Assert.Single(_publisher.Publicados);
    }

    [Fact]
    public async Task Procesar_PeriodoYaCargado_RechazaLaCargaCompleta()
    {
        _repository.RespuestaPeriodo = new ValidacionPeriodo
        {
            Resultado = ValidacionPeriodo.Rechazado, Periodo = "2025-02", CargaConflicto = 7, EstadoConflicto = "Notificado"
        };
        var processor = CreateProcessor("carga_masiva_productos.xlsx");

        await processor.ProcesarAsync(new CargaRegistradaMessage(2, "seaweed://uploads/a.xlsx", "admin@test.com"), CancellationToken.None);

        Assert.Empty(_repository.Insertados);
        Assert.Equal(ResultadoCarga.Rechazado, _repository.ResultadoFinal);
        Assert.Contains(_repository.Fallidos, f => f.Motivo == MotivoFallo.PeriodoRechazado);
        Assert.Single(_publisher.Publicados);
    }

    [Fact]
    public async Task Procesar_MensajeRepetido_NoVuelveAProcesar()
    {
        _repository.RespuestaInicio = InicioProceso.Omitir;
        var processor = CreateProcessor("carga_masiva_productos.xlsx");

        await processor.ProcesarAsync(new CargaRegistradaMessage(3, "seaweed://uploads/a.xlsx", "admin@test.com"), CancellationToken.None);

        Assert.Null(_repository.ResultadoFinal);
        Assert.Empty(_publisher.Publicados);
    }

    [Fact]
    public async Task Procesar_CargaFinalizadaSinNotificar_SoloVuelveAPublicarLaNotificacion()
    {
        _repository.RespuestaInicio = InicioProceso.ReenviarNotificacion;
        var processor = CreateProcessor("carga_masiva_productos.xlsx");

        await processor.ProcesarAsync(new CargaRegistradaMessage(5, "seaweed://uploads/a.xlsx", "admin@test.com"), CancellationToken.None);

        Assert.Null(_repository.ResultadoFinal);
        Assert.Empty(_repository.Insertados);
        var notificacion = Assert.IsType<CargaFinalizadaMessage>(Assert.Single(_publisher.Publicados));
        Assert.Equal(5, notificacion.IdCarga);
    }

    [Fact]
    public async Task Procesar_ArchivoQueNoEsExcel_FinalizaComoRechazado()
    {
        var processor = CreateProcessor("no_es_excel.xlsx");

        await processor.ProcesarAsync(new CargaRegistradaMessage(4, "seaweed://uploads/a.xlsx", "admin@test.com"), CancellationToken.None);

        Assert.Equal(ResultadoCarga.Rechazado, _repository.ResultadoFinal);
        Assert.Single(_publisher.Publicados);
    }

    private CargaProcessor CreateProcessor(string sampleFile)
    {
        var options = Options.Create(new ProcesamientoOptions());

        return new CargaProcessor(
            _repository,
            new FakeStorage(Path.Combine("samples", sampleFile)),
            new ExcelReader(options),
            new RegistroValidator(),
            _publisher,
            TimeProvider.System,
            NullLogger<CargaProcessor>.Instance);
    }

    private sealed class FakeStorage(string path) : IFileStorage
    {
        public Task<Stream> DownloadAsync(string rutaArchivo, CancellationToken cancellationToken)
        {
            Stream stream = new MemoryStream(File.ReadAllBytes(path));
            return Task.FromResult(stream);
        }
    }

    private sealed class FakePublisher : IMessagePublisher
    {
        public List<object?> Publicados { get; } = [];

        public Task PublishAsync<TMessage>(TMessage message, string routingKey, string correlationId, CancellationToken cancellationToken)
        {
            Publicados.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRepository : ICargaRepository
    {
        public string RespuestaInicio { get; set; } = InicioProceso.Ok;
        public ValidacionPeriodo RespuestaPeriodo { get; set; } = new() { Resultado = ValidacionPeriodo.Ok };
        public List<RegistroProducto> Insertados { get; } = [];
        public List<RegistroFallido> Fallidos { get; } = [];
        public bool MarcadoCargado { get; private set; }
        public string? ResultadoFinal { get; private set; }

        public Task<string> IniciarProcesoAsync(int idCarga, CancellationToken cancellationToken) => Task.FromResult(RespuestaInicio);

        public Task<ValidacionPeriodo> ValidarPeriodosAsync(int idCarga, IReadOnlyCollection<string> periodos, CancellationToken cancellationToken) =>
            Task.FromResult(RespuestaPeriodo);

        public Task<IReadOnlyList<ResultadoInsercion>> InsertarRegistrosAsync(int idCarga, IReadOnlyCollection<RegistroProducto> registros, CancellationToken cancellationToken)
        {
            Insertados.AddRange(registros);
            IReadOnlyList<ResultadoInsercion> resultado = registros
                .Select(r => new ResultadoInsercion { Fila = r.Fila, CodigoProducto = r.CodigoProducto, Insertado = true })
                .ToList();
            return Task.FromResult(resultado);
        }

        public Task RegistrarFallidosAsync(int idCarga, IReadOnlyCollection<RegistroFallido> fallidos, CancellationToken cancellationToken)
        {
            Fallidos.AddRange(fallidos);
            return Task.CompletedTask;
        }

        public Task MarcarCargadoAsync(int idCarga, int registrosInsertados, CancellationToken cancellationToken)
        {
            MarcadoCargado = true;
            return Task.CompletedTask;
        }

        public Task FinalizarAsync(int idCarga, int total, int insertados, int fallidos, string resultado, string? mensaje, CancellationToken cancellationToken)
        {
            ResultadoFinal = resultado;
            return Task.CompletedTask;
        }
    }
}
