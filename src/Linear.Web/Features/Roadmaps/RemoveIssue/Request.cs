namespace Linear.Web.Features.Roadmaps.RemoveIssue;

public sealed class RemoveRoadmapItemIssueRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid RoadmapId { get; set; }

    public Guid ItemId { get; set; }

    public string Identifier { get; set; } = string.Empty;
}
