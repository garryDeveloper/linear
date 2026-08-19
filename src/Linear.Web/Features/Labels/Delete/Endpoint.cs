using FastEndpoints;

using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Labels.Delete;

/// <summary>
/// <c>DELETE /api/teams/{key}/labels/{labelId}</c> — requiere rol Admin u Owner.
/// </summary>
public sealed class DeleteLabelEndpoint(DeleteLabelHandler handler) : Endpoint<DeleteLabelRequest>
{
    public override void Configure()
    {
        Delete("teams/{key}/labels/{labelId}");
    }

    public override async Task HandleAsync(DeleteLabelRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
