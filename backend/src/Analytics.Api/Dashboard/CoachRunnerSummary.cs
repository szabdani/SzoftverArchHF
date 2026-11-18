using Analytics.Api.Risk;

namespace Analytics.Api.Dashboard;

/// <summary>
/// Egy hozzárendelt futó az edző összesítő nézetében (UC14): utolsó edzés, a heti km (hétfőtől)
/// és a legutóbbi kockázati pontszám. A futó nevét a frontend az Identity kapcsolatlistájából veszi.
/// </summary>
public record CoachRunnerSummary(
    Guid RunnerId,
    DateOnly? LastWorkoutDate,
    decimal WeeklyKm,
    int? RiskScore,
    RiskCategory? RiskCategory);
