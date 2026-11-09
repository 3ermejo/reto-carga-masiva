namespace Control.Api.Application.Common;

/// <summary>Resultado de una operacion de negocio sin usar excepciones para los casos esperados.</summary>
public sealed class OperationResult<T>
{
    private OperationResult()
    {
    }

    public bool Succeeded { get; private init; }
    public T? Value { get; private init; }
    public string? Error { get; private init; }
    public int StatusCode { get; private init; }

    public static OperationResult<T> Success(T value) =>
        new() { Succeeded = true, Value = value, StatusCode = StatusCodes.Status200OK };

    public static OperationResult<T> Failure(string error, int statusCode) =>
        new() { Succeeded = false, Error = error, StatusCode = statusCode };
}
