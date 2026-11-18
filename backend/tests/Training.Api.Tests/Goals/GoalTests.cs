using Training.Api.Goals;

namespace Training.Api.Tests.Goals;

/// <summary>
/// A cél számított értékeinek tesztjei.
/// </summary>
public class GoalTests
{
    /// <summary>
    /// A hátralévő hetek felfelé kerekítve számolódnak; múltbeli vagy mai céldátumnál 0.
    /// </summary>
    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(7, 1)]
    [InlineData(8, 2)]
    [InlineData(10, 2)]
    [InlineData(84, 12)]
    public void WeeksUntil_RoundsUp(int daysFromToday, int expectedWeeks)
    {
        var today = new DateOnly(2026, 11, 18);

        Assert.Equal(expectedWeeks, Goal.WeeksUntil(today.AddDays(daysFromToday), today));
    }

    /// <summary>
    /// Új cél kitűzésekor aktív, archiváláskor inaktív lesz, és megkapja az archiválás idejét.
    /// </summary>
    [Fact]
    public void Archive_DeactivatesGoal()
    {
        var now = DateTimeOffset.UtcNow;
        var goal = Goal.Create(Guid.NewGuid(), new ValidGoalInput(GoalType.TenK, 10m, new DateOnly(2027, 3, 1), null), now);

        Assert.True(goal.IsActive);

        goal.Archive(now);

        Assert.False(goal.IsActive);
        Assert.Equal(now, goal.ArchivedAt);
    }
}
