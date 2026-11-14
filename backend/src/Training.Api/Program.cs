using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Persistence;
using Training.Api.CoachLinks;
using Training.Api.Persistence;
using Training.Api.Workouts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddPostgresDbContext<TrainingDbContext>(builder.Configuration);
builder.Services.AddMessaging<TrainingDbContext>(builder.Configuration, "training", bus =>
{
    bus.AddConsumer<CoachRunnerLinkConsumer>();
});

var app = builder.Build();

app.UseServiceDefaults();
app.MapWorkoutEndpoints();

await app.MigrateDatabaseAsync<TrainingDbContext>();
await app.RunAsync();
