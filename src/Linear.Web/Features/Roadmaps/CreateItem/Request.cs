namespace Linear.Web.Features.Roadmaps.CreateItem;

public sealed class CreateRoadmapItemRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid RoadmapId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly TargetDate { get; set; }
}
