namespace Linear.Web.Features.Roadmaps.Update;

public sealed class UpdateRoadmapRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid RoadmapId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
