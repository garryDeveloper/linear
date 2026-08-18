using FastEndpoints;

using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Teams.AddMember;

/// <summary>
/// <c>POST /api/teams/{key}/members</c> — requiere rol Admin u Owner en el equipo.
/// </summary>
public sealed class AddTeamMemberEndpoint(AddTeamMemberHandler handler)
    : Endpoint<AddTeamMemberRequest, TeamResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/members");
    }

    public override async Task HandleAsync(AddTeamMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
