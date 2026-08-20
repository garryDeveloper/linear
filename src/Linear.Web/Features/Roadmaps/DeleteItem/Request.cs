namespace Linear.Web.Features.Roadmaps.DeleteItem;

public sealed class DeleteRoadmapItemRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid RoadmapId { get; set; }

    public Guid ItemId { get; set; }
}
