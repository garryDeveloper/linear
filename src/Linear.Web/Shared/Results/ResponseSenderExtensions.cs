using FastEndpoints;

using Linear.Domain.Common;

namespace Linear.Web.Shared.Results;

/// <summary>
/// Envía un <see cref="Result{TValue}"/> como respuesta HTTP.
/// Concentra acá la traducción para que ningún endpoint elija status codes por su cuenta.
/// </summary>
public static class ResponseSenderExtensions
{
    /// <summary>
    /// Responde con el valor del resultado y <c>200 OK</c>, o con el error del resultado
    /// y el status que le corresponda a su <see cref="ErrorType"/>.
    /// </summary>
    public static Task SendResultAsync<TValue>(
        this IResponseSender sender,
        Result<TValue> result,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? sender.HttpContext.Response.SendAsync(
                result.Value,
                StatusCodes.Status200OK,
                cancellation: cancellationToken)
            : sender.SendErrorAsync(result.Error, cancellationToken);
    }

    /// <summary>
    /// Responde con <c>204 No Content</c> si el resultado es exitoso, o con su error.
    /// </summary>
    public static Task SendResultAsync(
        this IResponseSender sender,
        Result result,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? sender.HttpContext.Response.SendNoContentAsync(cancellationToken)
            : sender.SendErrorAsync(result.Error, cancellationToken);
    }

    private static Task SendErrorAsync(
        this IResponseSender sender,
        Error error,
        CancellationToken cancellationToken) =>
        sender.HttpContext.Response.SendAsync(
            new ApiError(error.Code, error.Description),
            ErrorHttpMapping.ToStatusCode(error),
            cancellation: cancellationToken);
}
