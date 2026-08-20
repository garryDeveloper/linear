using Linear.Domain.Roadmaps;

namespace Linear.UnitTests.Roadmaps;

public class RoadmapTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddDays(1);

    private static readonly Guid TeamId = Guid.CreateVersion7();
    private static readonly DateOnly Start = new(2026, 9, 1);
    private static readonly DateOnly Target = new(2026, 11, 30);

    private static Roadmap ARoadmap() =>
        Roadmap.Create(TeamId, "Segundo semestre", "Lo grande del semestre", Now).Value;

    private static (Roadmap Roadmap, RoadmapItem Item) ARoadmapWithAnItem()
    {
        var roadmap = ARoadmap();
        var item = roadmap.AddItem("Autenticación", "SSO y 2FA", Start, Target, Now).Value;

        return (roadmap, item);
    }

    [Fact]
    public void ANewRoadmapHasNoItems()
    {
        var roadmap = Roadmap.Create(TeamId, "Segundo semestre", "Lo grande", Now);

        Assert.True(roadmap.IsSuccess);
        Assert.Equal(TeamId, roadmap.Value.TeamId);
        Assert.Equal("Segundo semestre", roadmap.Value.Name);
        Assert.Empty(roadmap.Value.Items);
        Assert.Equal(Now, roadmap.Value.CreatedAt);
    }

    [Fact]
    public void TheNameAndDescriptionAreTrimmed()
    {
        var roadmap = Roadmap.Create(TeamId, "  Segundo semestre  ", "  Lo grande  ", Now);

        Assert.Equal("Segundo semestre", roadmap.Value.Name);
        Assert.Equal("Lo grande", roadmap.Value.Description);
    }

    [Fact]
    public void AnEmptyDescriptionIsStoredAsNull()
    {
        Assert.Null(Roadmap.Create(TeamId, "Segundo semestre", "   ", Now).Value.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ARoadmapWithoutANameIsRejected(string name)
    {
        var roadmap = Roadmap.Create(TeamId, name, null, Now);

        Assert.True(roadmap.IsFailure);
        Assert.Equal(RoadmapErrors.NameRequired, roadmap.Error);
    }

    [Fact]
    public void ANameLongerThanTheLimitIsRejected()
    {
        var roadmap = Roadmap.Create(TeamId, new string('a', Roadmap.MaxNameLength + 1), null, Now);

        Assert.Equal(RoadmapErrors.NameTooLong, roadmap.Error);
    }

    [Fact]
    public void UpdatingChangesTheNameAndDescription()
    {
        var roadmap = ARoadmap();

        var updated = roadmap.Update("Otro nombre", "Otra descripción", Later);

        Assert.True(updated.IsSuccess);
        Assert.Equal("Otro nombre", roadmap.Name);
        Assert.Equal("Otra descripción", roadmap.Description);
        Assert.Equal(Later, roadmap.UpdatedAt);
    }

    // ---- iniciativas -------------------------------------------------------------------

    [Fact]
    public void ANewItemStartsPlanned()
    {
        var (roadmap, item) = ARoadmapWithAnItem();

        Assert.Equal(RoadmapItemStatus.Planned, item.Status);
        Assert.Equal(roadmap.Id, item.RoadmapId);
        Assert.Equal(Start, item.StartDate);
        Assert.Equal(Target, item.TargetDate);
        Assert.Same(item, Assert.Single(roadmap.Items));
    }

    [Fact]
    public void AddingAnItemTouchesTheRoadmap()
    {
        var roadmap = ARoadmap();

        roadmap.AddItem("Autenticación", null, Start, Target, Later);

        Assert.Equal(Later, roadmap.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AnItemWithoutANameIsRejected(string name)
    {
        var roadmap = ARoadmap();

        var item = roadmap.AddItem(name, null, Start, Target, Now);

        Assert.True(item.IsFailure);
        Assert.Equal(RoadmapErrors.ItemNameRequired, item.Error);
        Assert.Empty(roadmap.Items);
    }

    /// <summary>La fecha objetivo tiene que ser posterior a la de inicio; iguales no alcanza.</summary>
    [Fact]
    public void AnItemWhoseTargetIsNotAfterItsStartIsRejected()
    {
        var roadmap = ARoadmap();

        var sameDay = roadmap.AddItem("Autenticación", null, Start, Start, Now);
        var backwards = roadmap.AddItem("Autenticación", null, Target, Start, Now);

        Assert.Equal(RoadmapErrors.TargetDateNotAfterStartDate, sameDay.Error);
        Assert.Equal(RoadmapErrors.TargetDateNotAfterStartDate, backwards.Error);
        Assert.Empty(roadmap.Items);
    }

    [Fact]
    public void AnItemCanBeUpdated()
    {
        var (roadmap, item) = ARoadmapWithAnItem();
        var newTarget = Target.AddMonths(1);

        var updated = roadmap.UpdateItem(item.Id, "SSO", "Solo SSO", Start, newTarget, Later);

        Assert.True(updated.IsSuccess);
        Assert.Equal("SSO", item.Name);
        Assert.Equal("Solo SSO", item.Description);
        Assert.Equal(newTarget, item.TargetDate);
        Assert.Equal(Later, item.UpdatedAt);
    }

    [Fact]
    public void UpdatingAnItemWithInvalidDatesLeavesItIntact()
    {
        var (roadmap, item) = ARoadmapWithAnItem();

        var updated = roadmap.UpdateItem(item.Id, "SSO", null, Target, Start, Later);

        Assert.True(updated.IsFailure);
        Assert.Equal(RoadmapErrors.TargetDateNotAfterStartDate, updated.Error);
        Assert.Equal("Autenticación", item.Name);
        Assert.Equal(Start, item.StartDate);
    }

    [Fact]
    public void UpdatingAnItemThatDoesNotExistFails()
    {
        var roadmap = ARoadmap();
        var missing = Guid.CreateVersion7();

        var updated = roadmap.UpdateItem(missing, "SSO", null, Start, Target, Later);

        Assert.True(updated.IsFailure);
        Assert.Equal(RoadmapErrors.ItemNotFound(missing), updated.Error);
    }

    /// <summary>
    /// El roadmap no define un recorrido obligatorio de estados: una iniciativa puede volver
    /// atrás si se despriorizó, o reabrirse después de darse por terminada.
    /// </summary>
    [Theory]
    [InlineData(RoadmapItemStatus.InProgress)]
    [InlineData(RoadmapItemStatus.Completed)]
    [InlineData(RoadmapItemStatus.Canceled)]
    [InlineData(RoadmapItemStatus.Planned)]
    public void AnItemCanMoveToAnyStatus(RoadmapItemStatus status)
    {
        var (roadmap, item) = ARoadmapWithAnItem();
        roadmap.ChangeItemStatus(item.Id, RoadmapItemStatus.Completed, Now);

        var changed = roadmap.ChangeItemStatus(item.Id, status, Later);

        Assert.True(changed.IsSuccess);
        Assert.Equal(status, item.Status);
    }

    [Fact]
    public void AnItemCanBeRemoved()
    {
        var (roadmap, item) = ARoadmapWithAnItem();

        var removed = roadmap.RemoveItem(item.Id, Later);

        Assert.True(removed.IsSuccess);
        Assert.Empty(roadmap.Items);
        Assert.Equal(Later, roadmap.UpdatedAt);
    }

    [Fact]
    public void RemovingAnItemThatDoesNotExistFails()
    {
        var roadmap = ARoadmap();
        var missing = Guid.CreateVersion7();

        var removed = roadmap.RemoveItem(missing, Later);

        Assert.True(removed.IsFailure);
        Assert.Equal(RoadmapErrors.ItemNotFound(missing), removed.Error);
    }

    [Fact]
    public void SeveralItemsCoexist()
    {
        var roadmap = ARoadmap();

        roadmap.AddItem("Autenticación", null, Start, Target, Now);
        roadmap.AddItem("Dashboard", null, Start.AddMonths(1), Target.AddMonths(1), Now);
        roadmap.AddItem("Mobile", null, Start.AddMonths(2), Target.AddMonths(2), Now);

        Assert.Equal(3, roadmap.Items.Count);
    }

    [Fact]
    public void TheTeamOfARoadmapNeverChanges()
    {
        var roadmap = ARoadmap();

        roadmap.Update("Otro nombre", null, Later);
        roadmap.AddItem("Autenticación", null, Start, Target, Later);

        Assert.Equal(TeamId, roadmap.TeamId);
    }
}
