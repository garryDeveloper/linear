using FastEndpoints;

using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Diagnostics.Health;

/// <summary>
/// <c>GET /api/health</c>
/// </summary>
public sealed class HealthEndpoint(HealthHandler handler) : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("health");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
