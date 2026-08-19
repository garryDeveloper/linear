using FastEndpoints;

using Linear.Web.Features.Labels.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Labels.Update;

/// <summary>
/// <c>PUT /api/teams/{key}/labels/{labelId}</c> — requiere rol Admin u Owner.
/// </summary>
public sealed class UpdateLabelEndpoint(UpdateLabelHandler handler)
    : Endpoint<UpdateLabelRequest, LabelResponse>
{
    public override void Configure()
    {
        Put("teams/{key}/labels/{labelId}");
    }

    public override async Task HandleAsync(UpdateLabelRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
