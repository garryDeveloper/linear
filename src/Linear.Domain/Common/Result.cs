namespace Linear.Domain.Common;

/// <summary>
/// Resultado de una operación que puede fallar sin que el fallo sea excepcional.
/// Es el mecanismo estándar del proyecto: las validaciones y las reglas de negocio
/// devuelven <see cref="Result"/>, no lanzan excepciones
/// (.ai/coding-standards.md).
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        // Un resultado exitoso con error, o fallido sin error, es un bug de programación
        // —no una validación—, así que acá sí corresponde una excepción.
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Un resultado exitoso no puede tener un error.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("Un resultado fallido debe tener un error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>
/// Resultado que transporta un valor cuando la operación es exitosa.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Valor producido por la operación.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si el resultado es fallido. Verificá <see cref="Result.IsSuccess"/> antes de leerlo.
    /// </exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede acceder al valor de un resultado fallido.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
