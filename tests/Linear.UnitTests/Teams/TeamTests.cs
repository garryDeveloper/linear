using Linear.Domain.Teams;

namespace Linear.UnitTests.Teams;

public class TeamTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Owner = Guid.CreateVersion7();
    private static readonly Guid Other = Guid.CreateVersion7();

    private static TeamKey AKey() => TeamKey.Create("WEB").Value;

    private static Team ATeam() =>
        Team.Create("Web", AKey(), "El equipo de la web", Owner, Now).Value;

    [Fact]
    public void ANewTeamIsBornWithItsCreatorAsOwner()
    {
        var team = Team.Create("Web", AKey(), "Descripción", Owner, Now);

        Assert.True(team.IsSuccess);
        Assert.Equal("Web", team.Value.Name);
        Assert.Equal("WEB", team.Value.Key.Value);
        Assert.Equal(Now, team.Value.CreatedAt);

        var member = Assert.Single(team.Value.Members);
        Assert.Equal(Owner, member.UserId);
        Assert.Equal(TeamRole.Owner, member.Role);
    }

    [Fact]
    public void TheNameIsTrimmed()
    {
        var team = Team.Create("  Web  ", AKey(), null, Owner, Now);

        Assert.Equal("Web", team.Value.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ATeamWithoutANameIsRejected(string name)
    {
        var team = Team.Create(name, AKey(), null, Owner, Now);

        Assert.True(team.IsFailure);
        Assert.Equal(TeamErrors.NameRequired, team.Error);
    }

    [Fact]
    public void ANameLongerThanTheLimitIsRejected()
    {
        var team = Team.Create(new string('a', Team.MaxNameLength + 1), AKey(), null, Owner, Now);

        Assert.Equal(TeamErrors.NameTooLong, team.Error);
    }

    [Fact]
    public void ADescriptionLongerThanTheLimitIsRejected()
    {
        var team = Team.Create("Web", AKey(), new string('a', Team.MaxDescriptionLength + 1), Owner, Now);

        Assert.Equal(TeamErrors.DescriptionTooLong, team.Error);
    }

    [Fact]
    public void AnEmptyDescriptionIsStoredAsNull()
    {
        var team = Team.Create("Web", AKey(), "   ", Owner, Now);

        Assert.Null(team.Value.Description);
    }

    [Fact]
    public void UpdatingChangesTheNameAndTheTimestamp()
    {
        var team = ATeam();
        var later = Now.AddHours(1);

        var result = team.Update("Web renovado", "Otra descripción", later);

        Assert.True(result.IsSuccess);
        Assert.Equal("Web renovado", team.Name);
        Assert.Equal("Otra descripción", team.Description);
        Assert.Equal(later, team.UpdatedAt);
    }

    [Fact]
    public void UpdatingWithAnInvalidNameLeavesTheTeamUntouched()
    {
        var team = ATeam();

        var result = team.Update("", null, Now.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("Web", team.Name);
        Assert.Equal(Now, team.UpdatedAt);
    }

    [Fact]
    public void AMemberCanBeAdded()
    {
        var team = ATeam();

        var result = team.AddMember(Other, TeamRole.Member, Now.AddHours(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, team.Members.Count);
        Assert.Equal(TeamRole.Member, team.RoleOf(Other));
    }

    [Fact]
    public void TheSameUserCannotJoinTwice()
    {
        var team = ATeam();
        team.AddMember(Other, TeamRole.Member, Now);

        var result = team.AddMember(Other, TeamRole.Admin, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(TeamErrors.AlreadyMember, result.Error);
        Assert.Equal(2, team.Members.Count);
    }

    [Fact]
    public void RemovingSomeoneWhoIsNotAMemberFails()
    {
        var team = ATeam();

        var result = team.RemoveMember(Other, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(TeamErrors.MemberNotFound, result.Error);
    }

    [Fact]
    public void TheOnlyOwnerCannotBeRemoved()
    {
        var team = ATeam();
        team.AddMember(Other, TeamRole.Admin, Now);

        var result = team.RemoveMember(Owner, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(TeamErrors.LastOwner, result.Error);
        Assert.Equal(TeamRole.Owner, team.RoleOf(Owner));
    }

    [Fact]
    public void AnOwnerCanBeRemovedIfAnotherOwnerRemains()
    {
        var team = ATeam();
        team.AddMember(Other, TeamRole.Owner, Now);

        var result = team.RemoveMember(Owner, Now);

        Assert.True(result.IsSuccess);
        Assert.Null(team.RoleOf(Owner));
        Assert.Equal(TeamRole.Owner, team.RoleOf(Other));
    }

    [Fact]
    public void TheOnlyOwnerCannotBeDemoted()
    {
        var team = ATeam();
        team.AddMember(Other, TeamRole.Admin, Now);

        var result = team.ChangeMemberRole(Owner, TeamRole.Admin, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(TeamErrors.LastOwner, result.Error);
        Assert.Equal(TeamRole.Owner, team.RoleOf(Owner));
    }

    [Fact]
    public void AMemberCanBePromoted()
    {
        var team = ATeam();
        team.AddMember(Other, TeamRole.Member, Now);
        var later = Now.AddHours(1);

        var result = team.ChangeMemberRole(Other, TeamRole.Admin, later);

        Assert.True(result.IsSuccess);
        Assert.Equal(TeamRole.Admin, team.RoleOf(Other));
        Assert.Equal(later, team.UpdatedAt);
    }

    [Fact]
    public void AssigningTheSameRoleIsANoOp()
    {
        var team = ATeam();
        team.AddMember(Other, TeamRole.Member, Now);

        var result = team.ChangeMemberRole(Other, TeamRole.Member, Now.AddHours(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(Now, team.UpdatedAt);
    }

    [Fact]
    public void ChangingTheRoleOfSomeoneWhoIsNotAMemberFails()
    {
        var team = ATeam();

        var result = team.ChangeMemberRole(Other, TeamRole.Admin, Now);

        Assert.Equal(TeamErrors.MemberNotFound, result.Error);
    }

    [Fact]
    public void RoleOfReturnsNullForSomeoneOutsideTheTeam()
    {
        Assert.Null(ATeam().RoleOf(Other));
        Assert.False(ATeam().HasMember(Other));
    }

    [Fact]
    public void TheRoleHierarchyIsOrdered()
    {
        // El guardián de permisos compara roles con >=, así que el orden del enum es
        // parte del contrato y no un detalle cosmético.
        Assert.True(TeamRole.Owner > TeamRole.Admin);
        Assert.True(TeamRole.Admin > TeamRole.Member);
    }
}
