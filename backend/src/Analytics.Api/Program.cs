using Analytics.Api.CoachLinks;
using Analytics.Api.Persistence;
using Analytics.Api.ReadModels;
using Analytics.Api.Risk;
using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddPostgresDbContext<AnalyticsDbContext>(builder.Configuration);
builder.Services.AddMessaging<AnalyticsDbContext>(builder.Configuration, "analytics", bus =>
{
    bus.AddConsumer<TrainingEventsConsumer>();
    bus.AddConsumer<CoachRunnerLinkConsumer>();
});
builder.Services.AddScoped<IRiskService, RiskService>();
builder.Services.AddHostedService<DailyRiskJob>();

var app = builder.Build();

app.UseServiceDefaults();
app.MapRiskEndpoints();

await app.MigrateDatabaseAsync<AnalyticsDbContext>();
await app.RunAsync();
