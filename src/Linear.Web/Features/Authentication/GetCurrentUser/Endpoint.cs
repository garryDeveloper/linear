using FastEndpoints;

using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Authentication.GetCurrentUser;

/// <summary>
/// <c>GET /api/auth/me</c> — identidad del usuario autenticado.
/// </summary>
/// <remarks>
/// FastEndpoints exige autorización salvo que se declare <c>AllowAnonymous</c>, así que
/// este endpoint solo responde con una sesión válida.
/// </remarks>
public sealed class GetCurrentUserEndpoint(GetCurrentUserHandler handler)
    : EndpointWithoutRequest<CurrentUserResponse>
{
    public override void Configure()
    {
        Get("auth/me");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
