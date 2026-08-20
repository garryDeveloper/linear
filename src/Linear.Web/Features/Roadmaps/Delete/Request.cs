namespace Linear.Web.Features.Roadmaps.Delete;

public sealed class DeleteRoadmapRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid RoadmapId { get; set; }
}
