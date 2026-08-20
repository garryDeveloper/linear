namespace Linear.Web.Features.Roadmaps.GetById;

public sealed class GetRoadmapByIdRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid RoadmapId { get; set; }
}
