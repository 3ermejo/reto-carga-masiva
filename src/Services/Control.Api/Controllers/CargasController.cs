using System.ComponentModel.DataAnnotations;
using Control.Api.Application.Common;
using Control.Api.Application.Dtos;
using Control.Api.Application.Interfaces;
using Control.Api.Domain;
using Control.Api.Extensions;
using Control.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Control.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cargas")]
public class CargasController : ControllerBase
{
    private readonly ICargaService _cargaService;

    public CargasController(ICargaService cargaService)
    {
        _cargaService = cargaService;
    }

    /// <summary>Recibe el Excel, lo registra como Pendiente, lo guarda en SeaweedFS y lo encola.</summary>
    [HttpPost]
    [Authorize(Policy = ServiceCollectionExtensions.PuedeCargarPolicy)]
    [ProducesResponseType(typeof(CargaAceptadaResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Subir(IFormFile? archivo, CancellationToken cancellationToken)
    {
        var result = await _cargaService.RegistrarAsync(archivo, User.ToCurrentUser(), cancellationToken);

        if (!result.Succeeded)
            return Problem(title: result.Error, statusCode: result.StatusCode);

        return Accepted(result.Value);
    }

    /// <summary>Historial de cargas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CargaResumen>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return Ok(await _cargaService.ListarAsync(User.ToCurrentUser(), cancellationToken));
    }

    /// <summary>Detalle del estado de una carga con su historial (auditoria).</summary>
    [HttpGet("{idCarga:int}")]
    [ProducesResponseType(typeof(CargaDetalleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int idCarga, CancellationToken cancellationToken)
    {
        var detalle = await _cargaService.ObtenerDetalleAsync(idCarga, User.ToCurrentUser(), cancellationToken);
        return detalle is null ? NotFound() : Ok(detalle);
    }

    /// <summary>Contenido del Excel que se registro en la base de datos (paginado).</summary>
    [HttpGet("{idCarga:int}/registros")]
    [ProducesResponseType(typeof(PagedResult<RegistroProcesado>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Registros(
        int idCarga,
        [FromQuery, Range(1, int.MaxValue)] int pagina = 1,
        [FromQuery, Range(1, 200)] int tamano = 50,
        CancellationToken cancellationToken = default)
    {
        var registros = await _cargaService.ListarRegistrosAsync(idCarga, pagina, tamano, User.ToCurrentUser(), cancellationToken);
        return registros is null ? NotFound() : Ok(registros);
    }

    /// <summary>Registros que no se cargaron y su motivo (Existente, DatoInvalido, PeriodoRechazado).</summary>
    [HttpGet("{idCarga:int}/fallidos")]
    [ProducesResponseType(typeof(PagedResult<RegistroFallido>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Fallidos(
        int idCarga,
        [FromQuery, Range(1, int.MaxValue)] int pagina = 1,
        [FromQuery, Range(1, 200)] int tamano = 50,
        CancellationToken cancellationToken = default)
    {
        var fallidos = await _cargaService.ListarFallidosAsync(idCarga, pagina, tamano, User.ToCurrentUser(), cancellationToken);
        return fallidos is null ? NotFound() : Ok(fallidos);
    }

    /// <summary>Descarga el Excel original desde SeaweedFS.</summary>
    [HttpGet("{idCarga:int}/archivo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Descargar(int idCarga, CancellationToken cancellationToken)
    {
        var archivo = await _cargaService.DescargarArchivoAsync(idCarga, User.ToCurrentUser(), cancellationToken);
        return archivo is null ? NotFound() : File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }
}
