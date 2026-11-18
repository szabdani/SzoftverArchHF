using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Persistence;
using Training.Api.CoachLinks;
using Training.Api.Goals;
using Training.Api.Persistence;
using Training.Api.Workouts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddPostgresDbContext<TrainingDbContext>(builder.Configuration);
builder.Services.AddMessaging<TrainingDbContext>(builder.Configuration, "training", bus =>
{
    bus.AddConsumer<CoachRunnerLinkConsumer>();
});
builder.Services.AddScoped<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<IGoalService, GoalService>();

var app = builder.Build();

app.UseServiceDefaults();
app.MapWorkoutEndpoints();
app.MapGoalEndpoints();

await app.MigrateDatabaseAsync<TrainingDbContext>();
await app.RunAsync();
