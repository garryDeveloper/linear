using FastEndpoints;

using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Teams.ChangeMemberRole;

/// <summary>
/// <c>PUT /api/teams/{key}/members/{userId}/role</c> — requiere rol Admin u Owner.
/// </summary>
public sealed class ChangeTeamMemberRoleEndpoint(ChangeTeamMemberRoleHandler handler)
    : Endpoint<ChangeTeamMemberRoleRequest, TeamResponse>
{
    public override void Configure()
    {
        Put("teams/{key}/members/{userId}/role");
    }

    public override async Task HandleAsync(
        ChangeTeamMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
