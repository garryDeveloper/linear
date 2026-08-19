using FastEndpoints;

using Linear.Web.Features.Labels.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Labels.Create;

/// <summary>
/// <c>POST /api/teams/{key}/labels</c> — requiere rol Admin u Owner en el equipo.
/// </summary>
public sealed class CreateLabelEndpoint(CreateLabelHandler handler)
    : Endpoint<CreateLabelRequest, LabelResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/labels");
    }

    public override async Task HandleAsync(CreateLabelRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
