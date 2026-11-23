using System.Security.Claims;
using BuildingBlocks.Auth;
using BuildingBlocks.Time;

namespace Training.Api.Workouts;

/// <summary>
/// Az edzések HTTP végpontjai. Csak a kérés ellenőrzését és a HTTP választ intézik, a logika a service-ben van.
/// </summary>
public static class WorkoutEndpoints
{
    /// <summary>
    /// Regisztrálja az edzés végpontokat a /api/training/workouts útvonalon.
    /// </summary>
    public static void MapWorkoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/training/workouts");

        group.MapGet("/", ListAsync).RequireAuthorization(Policies.RunnerOrCoach).Produces<List<WorkoutResponse>>();
        group.MapGet("/{id:guid}", GetAsync).RequireAuthorization(Policies.RunnerOrCoach).Produces<WorkoutResponse>();
        group.MapPost("/", CreateAsync).RequireAuthorization(Policies.Runner).Produces<WorkoutResponse>(StatusCodes.Status201Created).ProducesValidationProblem();
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(Policies.Runner).Produces<WorkoutResponse>().ProducesValidationProblem();
        group.MapDelete("/{id:guid}", DeleteAsync).RequireAuthorization(Policies.Runner).Produces(StatusCodes.Status204NoContent);
    }

    /// <summary>
    /// GET /api/training/workouts?runnerId=&amp;from=&amp;to= – edzések listája.
    /// </summary>
    private static async Task<IResult> ListAsync(
        Guid? runnerId, DateOnly? from, DateOnly? to, ClaimsPrincipal user, IWorkoutService service, CancellationToken ct)
    {
        var workouts = await service.ListAsync(user, runnerId, from, to, ct);
        return workouts is null ? Results.Forbid() : Results.Ok(workouts);
    }

    /// <summary>
    /// GET /api/training/workouts/{id} – egy edzés.
    /// </summary>
    private static async Task<IResult> GetAsync(Guid id, ClaimsPrincipal user, IWorkoutService service, CancellationToken ct)
    {
        var workout = await service.GetAsync(user, id, ct);
        return workout is null ? Results.NotFound() : Results.Ok(workout);
    }

    /// <summary>
    /// POST /api/training/workouts – új edzés rögzítése.
    /// </summary>
    private static async Task<IResult> CreateAsync(
        WorkoutRequest request, ClaimsPrincipal user, IWorkoutService service, TimeProvider time, CancellationToken ct)
    {
        if (!WorkoutValidator.TryValidate(request, time.GetLocalToday(), out var input, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var workout = await service.CreateAsync(user, input, ct);
        if (workout is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["plannedWorkoutId"] = ["A tervezett edzés nem található, vagy már teljesült."],
            });
        }

        return Results.Created($"/api/training/workouts/{workout.Id}", workout);
    }

    /// <summary>
    /// PUT /api/training/workouts/{id} – saját edzés módosítása.
    /// </summary>
    private static async Task<IResult> UpdateAsync(
        Guid id, WorkoutRequest request, ClaimsPrincipal user, IWorkoutService service, TimeProvider time, CancellationToken ct)
    {
        if (!WorkoutValidator.TryValidate(request, time.GetLocalToday(), out var input, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var workout = await service.UpdateAsync(user, id, input, ct);
        return workout is null ? Results.NotFound() : Results.Ok(workout);
    }

    /// <summary>
    /// DELETE /api/training/workouts/{id} – saját edzés törlése.
    /// </summary>
    private static async Task<IResult> DeleteAsync(Guid id, ClaimsPrincipal user, IWorkoutService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(user, id, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
