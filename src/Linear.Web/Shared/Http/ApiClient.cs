using System.Net.Http.Json;
using System.Text.Json;

using Linear.Domain.Common;
using Linear.Web.Shared.Results;

namespace Linear.Web.Shared.Http;

/// <summary>
/// Cliente de los endpoints internos de la aplicación.
/// Es el único camino por el que los componentes Blazor llegan al backend
/// (.ai/architecture.md § Communication), y traduce toda respuesta HTTP a
/// <see cref="Result{TValue}"/> para que la UI nunca maneje excepciones de red.
/// </summary>
public sealed class ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
{
    public async Task<Result<TResponse>> GetAsync<TResponse>(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeUrl);

        try
        {
            using var response = await httpClient.GetAsync(relativeUrl, cancellationToken);
            return await ReadResultAsync<TResponse>(response, relativeUrl, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancelación real del llamador (navegación, circuito cerrado): no es un fallo del API.
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(exception, "No se pudo contactar el API interno en {RelativeUrl}.", relativeUrl);

            return Result.Failure<TResponse>(Error.Failure(
                "Api.Unreachable",
                "No se pudo contactar el servicio."));
        }
    }

    private async Task<Result<TResponse>> ReadResultAsync<TResponse>(
        HttpResponseMessage response,
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var apiError = await TryReadApiErrorAsync(response, cancellationToken);
            var errorType = ErrorHttpMapping.ToErrorType(response.StatusCode);

            logger.LogWarning(
                "El API interno respondió {StatusCode} en {RelativeUrl}: {ErrorCode}",
                (int)response.StatusCode,
                relativeUrl,
                apiError?.Code);

            return Result.Failure<TResponse>(new Error(
                apiError?.Code ?? "Api.Error",
                apiError?.Description ?? "La operación no pudo completarse.",
                errorType));
        }

        var payload = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);

        return payload is null
            ? Result.Failure<TResponse>(Error.Failure("Api.EmptyResponse", "El servicio devolvió una respuesta vacía."))
            : Result.Success(payload);
    }

    private async Task<ApiError?> TryReadApiErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // El cuerpo del error no es un ApiError (por ejemplo, una página de error del host).
            return null;
        }
    }
}
