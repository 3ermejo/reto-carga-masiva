namespace Control.Api.Application.Dtos;

public record ArchivoDescarga(Stream Contenido, string NombreArchivo, string ContentType);
