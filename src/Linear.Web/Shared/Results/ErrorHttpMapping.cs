using System.Net;

using Linear.Domain.Common;

namespace Linear.Web.Shared.Results;

/// <summary>
/// Traduce entre la clasificación de errores del dominio y los códigos HTTP.
/// Es el único lugar del proyecto donde esa correspondencia está definida:
/// el dominio no conoce HTTP y los endpoints no deciden status codes por su cuenta.
/// </summary>
public static class ErrorHttpMapping
{
    /// <summary>Status HTTP con el que un endpoint responde ante un <see cref="Error"/>.</summary>
    public static int ToStatusCode(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return ToStatusCode(error.Type);
    }

    /// <inheritdoc cref="ToStatusCode(Error)"/>
    public static int ToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    /// <summary>
    /// Clasificación de dominio que corresponde a una respuesta HTTP fallida.
    /// La usa el cliente interno para reconstruir un <see cref="Error"/> a partir de la respuesta.
    /// </summary>
    public static ErrorType ToErrorType(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => ErrorType.Validation,
        HttpStatusCode.Unauthorized => ErrorType.Unauthorized,
        HttpStatusCode.Forbidden => ErrorType.Forbidden,
        HttpStatusCode.NotFound => ErrorType.NotFound,
        HttpStatusCode.Conflict => ErrorType.Conflict,
        _ => ErrorType.Failure
    };
}
