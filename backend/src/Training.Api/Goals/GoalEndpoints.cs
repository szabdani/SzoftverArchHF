using System.Security.Claims;
using BuildingBlocks.Auth;
using BuildingBlocks.Time;

namespace Training.Api.Goals;

/// <summary>
/// A célok HTTP végpontjai. Csak a kérés ellenőrzését és a HTTP választ intézik, a logika a service-ben van.
/// </summary>
public static class GoalEndpoints
{
    /// <summary>
    /// Regisztrálja a cél végpontokat a /api/training/goals útvonalon.
    /// </summary>
    public static void MapGoalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/training/goals");

        // A futó kezeli a céljait, az edző csak megtekinti (spec 3.1).
        group.MapGet("/", ListAsync).RequireAuthorization(Policies.RunnerOrCoach);
        group.MapGet("/active", GetActiveAsync).RequireAuthorization(Policies.RunnerOrCoach);
        group.MapPost("/", CreateAsync).RequireAuthorization(Policies.Runner);
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(Policies.Runner);
    }

    /// <summary>
    /// GET /api/training/goals?runnerId= – a futó összes célja.
    /// </summary>
    private static async Task<IResult> ListAsync(Guid? runnerId, ClaimsPrincipal user, IGoalService service, CancellationToken ct)
    {
        var goals = await service.ListAsync(user, runnerId, ct);
        return goals is null ? Results.Forbid() : Results.Ok(goals);
    }

    /// <summary>
    /// GET /api/training/goals/active?runnerId= – az aktív cél nézete.
    /// </summary>
    private static async Task<IResult> GetActiveAsync(Guid? runnerId, ClaimsPrincipal user, IGoalService service, CancellationToken ct)
    {
        var activeGoal = await service.GetActiveAsync(user, runnerId, ct);
        return activeGoal is null ? Results.NotFound() : Results.Ok(activeGoal);
    }

    /// <summary>
    /// POST /api/training/goals – új aktív cél kitűzése.
    /// </summary>
    private static async Task<IResult> CreateAsync(
        GoalRequest request, ClaimsPrincipal user, IGoalService service, TimeProvider time, CancellationToken ct)
    {
        if (!GoalValidator.TryValidate(request, time.GetLocalToday(), out var input, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        return Results.Ok(await service.CreateAsync(user, input, ct));
    }

    /// <summary>
    /// PUT /api/training/goals/{id} – a saját aktív cél módosítása.
    /// </summary>
    private static async Task<IResult> UpdateAsync(
        Guid id, GoalRequest request, ClaimsPrincipal user, IGoalService service, TimeProvider time, CancellationToken ct)
    {
        if (!GoalValidator.TryValidate(request, time.GetLocalToday(), out var input, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var goal = await service.UpdateAsync(user, id, input, ct);
        return goal is null ? Results.NotFound() : Results.Ok(goal);
    }
}
