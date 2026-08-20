namespace Linear.Domain.Activities;

/// <summary>Sobre qué entidad ocurrió la acción.</summary>
public enum ActivityEntityType
{
    Issue = 0,
    Comment = 1,
    Sprint = 2,
    RoadmapItem = 3,
    Label = 4,
    Team = 5
}
