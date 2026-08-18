using FastEndpoints;

using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Teams.RemoveMember;

/// <summary>
/// <c>DELETE /api/teams/{key}/members/{userId}</c> — requiere rol Admin u Owner.
/// </summary>
public sealed class RemoveTeamMemberEndpoint(RemoveTeamMemberHandler handler)
    : Endpoint<RemoveTeamMemberRequest, TeamResponse>
{
    public override void Configure()
    {
        Delete("teams/{key}/members/{userId}");
    }

    public override async Task HandleAsync(RemoveTeamMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
