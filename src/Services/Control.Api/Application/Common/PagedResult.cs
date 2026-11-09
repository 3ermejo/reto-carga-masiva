namespace Control.Api.Application.Common;

public record PagedResult<T>(IReadOnlyList<T> Items, int Pagina, int Tamano, long Total);
