using System.Security.Claims;
using BuildingBlocks.Auth;

namespace Training.Api.Plans;

/// <summary>
/// Az edzéstervek és tervezett edzések HTTP végpontjai. Csak a kérés ellenőrzését és a HTTP
/// választ intézik, a logika a service-ben van.
/// </summary>
public static class PlanEndpoints
{
    /// <summary>
    /// Regisztrálja a végpontokat a /api/training/plans és /api/training/planned-workouts útvonalakon.
    /// </summary>
    public static void MapPlanEndpoints(this IEndpointRouteBuilder app)
    {
        // A tervet a futó (saját magának) és az edző (hozzárendelt futónak) is kezelheti; a futónkénti
        // jogosultságot a service ellenőrzi.
        var plans = app.MapGroup("/api/training/plans").RequireAuthorization(Policies.RunnerOrCoach);

        plans.MapGet("/", ListAsync).Produces<List<PlanResponse>>();
        plans.MapGet("/{id:guid}", GetAsync).Produces<PlanResponse>();
        plans.MapPost("/", CreateAsync).Produces<PlanResponse>(StatusCodes.Status201Created).ProducesValidationProblem();
        plans.MapPut("/{id:guid}", UpdateAsync).Produces<PlanResponse>().ProducesValidationProblem();
        plans.MapDelete("/{id:guid}", DeleteAsync).Produces(StatusCodes.Status204NoContent);
        plans.MapPost("/{planId:guid}/workouts", AddPlannedWorkoutAsync).Produces<PlannedWorkoutResponse>().ProducesValidationProblem();
        plans.MapPut("/{planId:guid}/workouts/{id:guid}", UpdatePlannedWorkoutAsync).Produces<PlannedWorkoutResponse>().ProducesValidationProblem();
        plans.MapDelete("/{planId:guid}/workouts/{id:guid}", DeletePlannedWorkoutAsync).Produces(StatusCodes.Status204NoContent);

        // Naptár (UC07): a tervezett edzések egy időszakban; a teljesítetteket a /workouts adja.
        app.MapGet("/api/training/planned-workouts", ListPlannedWorkoutsAsync).RequireAuthorization(Policies.RunnerOrCoach).Produces<List<PlannedWorkoutResponse>>();
    }

    /// <summary>
    /// GET /api/training/plans?runnerId= – a futó tervei.
    /// </summary>
    private static async Task<IResult> ListAsync(Guid? runnerId, ClaimsPrincipal user, IPlanService service, CancellationToken ct)
    {
        var plans = await service.ListAsync(user, runnerId, ct);
        return plans is null ? Results.Forbid() : Results.Ok(plans);
    }

    /// <summary>
    /// GET /api/training/plans/{id} – egy terv a tervezett edzéseivel.
    /// </summary>
    private static async Task<IResult> GetAsync(Guid id, ClaimsPrincipal user, IPlanService service, CancellationToken ct)
    {
        var plan = await service.GetAsync(user, id, ct);
        return plan is null ? Results.NotFound() : Results.Ok(plan);
    }

    /// <summary>
    /// POST /api/training/plans – új terv (az edző a runnerId-vel adja meg a futót).
    /// </summary>
    private static async Task<IResult> CreateAsync(PlanRequest request, ClaimsPrincipal user, IPlanService service, CancellationToken ct)
    {
        if (!PlanValidator.TryValidatePlan(request, out var input, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var plan = await service.CreateAsync(user, request.RunnerId, input, ct);
        return plan is null ? Results.Forbid() : Results.Created($"/api/training/plans/{plan.Id}", plan);
    }

    /// <summary>
    /// PUT /api/training/plans/{id} – a terv adatainak módosítása.
    /// </summary>
    private static async Task<IResult> UpdateAsync(
        Guid id, PlanRequest request, ClaimsPrincipal user, IPlanService service, CancellationToken ct)
    {
        if (!PlanValidator.TryValidatePlan(request, out var input, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var plan = await service.UpdateAsync(user, id, input, ct);
        return plan is null ? Results.NotFound() : Results.Ok(plan);
    }

    /// <summary>
    /// DELETE /api/training/plans/{id} – a terv törlése a tervezett edzéseivel.
    /// </summary>
    private static async Task<IResult> DeleteAsync(Guid id, ClaimsPrincipal user, IPlanService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(user, id, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    /// <summary>
    /// POST /api/training/plans/{planId}/workouts – új tervezett edzés a tervbe.
    /// </summary>
    private static async Task<IResult> AddPlannedWorkoutAsync(
        Guid planId, PlannedWorkoutRequest request, ClaimsPrincipal user, IPlanService service, CancellationToken ct)
    {
        // A dátum ellenőrzéséhez kell a terv időszaka; ha a terv nem látható, 404.
        var plan = await service.GetAsync(user, planId, ct);
        if (plan is null)
        {
            return Results.NotFound();
        }

        if (!PlanValidator.TryValidatePlannedWorkout(request, plan.StartDate, plan.EndDate, out var input, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var plannedWorkout = await service.AddPlannedWorkoutAsync(user, planId, input, ct);
        return plannedWorkout is null ? Results.NotFound() : Results.Ok(plannedWorkout);
    }

    /// <summary>
    /// PUT /api/training/plans/{planId}/workouts/{id} – tervezett edzés módosítása vagy áthelyezése.
    /// </summary>
    private static async Task<IResult> UpdatePlannedWorkoutAsync(
        Guid planId, Guid id, PlannedWorkoutRequest request, ClaimsPrincipal user, IPlanService service, CancellationToken ct)
    {
        var plan = await service.GetAsync(user, planId, ct);
        if (plan is null)
        {
            return Results.NotFound();
        }

        if (!PlanValidator.TryValidatePlannedWorkout(request, plan.StartDate, plan.EndDate, out var input, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var plannedWorkout = await service.UpdatePlannedWorkoutAsync(user, planId, id, input, ct);
        return plannedWorkout is null ? Results.NotFound() : Results.Ok(plannedWorkout);
    }

    /// <summary>
    /// DELETE /api/training/plans/{planId}/workouts/{id} – tervezett edzés törlése.
    /// </summary>
    private static async Task<IResult> DeletePlannedWorkoutAsync(
        Guid planId, Guid id, ClaimsPrincipal user, IPlanService service, CancellationToken ct)
    {
        var deleted = await service.DeletePlannedWorkoutAsync(user, planId, id, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    /// <summary>
    /// GET /api/training/planned-workouts?runnerId=&amp;from=&amp;to= – tervezett edzések a naptárhoz.
    /// </summary>
    private static async Task<IResult> ListPlannedWorkoutsAsync(
        Guid? runnerId, DateOnly? from, DateOnly? to, ClaimsPrincipal user, IPlanService service, CancellationToken ct)
    {
        var plannedWorkouts = await service.ListPlannedWorkoutsAsync(user, runnerId, from, to, ct);
        return plannedWorkouts is null ? Results.Forbid() : Results.Ok(plannedWorkouts);
    }
}
