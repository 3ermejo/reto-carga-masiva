using BulkLoad.Worker.Domain;

namespace BulkLoad.Worker.Application.Validation;

public record ResultadoValidacion(RegistroProducto? Registro, string? Error)
{
    public bool EsValido => Error is null;

    public static ResultadoValidacion Valido(RegistroProducto registro) => new(registro, null);
    public static ResultadoValidacion Invalido(string error) => new(null, error);
}
