using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Persistence;
using Training.Api.CoachLinks;
using Training.Api.Demo;
using Training.Api.Goals;
using Training.Api.Persistence;
using Training.Api.Plans;
using Training.Api.Workouts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddPostgresDbContext<TrainingDbContext>(builder.Configuration);
builder.Services.AddMessaging<TrainingDbContext>(builder.Configuration, "training", bus =>
{
    bus.AddConsumer<CoachRunnerLinkConsumer>();
    bus.AddConsumer<DemoDataConsumer>();
});
builder.Services.AddScoped<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddHostedService<MissedWorkoutJob>();

var app = builder.Build();

app.UseServiceDefaults("training");
app.MapWorkoutEndpoints();
app.MapGoalEndpoints();
app.MapPlanEndpoints();

await app.MigrateDatabaseAsync<TrainingDbContext>();
await app.RunAsync();
