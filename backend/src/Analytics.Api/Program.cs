using Analytics.Api.Alerts;
using Analytics.Api.CoachLinks;
using Analytics.Api.Persistence;
using Analytics.Api.ReadModels;
using Analytics.Api.Recommendations;
using Analytics.Api.Risk;
using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddPostgresDbContext<AnalyticsDbContext>(builder.Configuration);
builder.Services.AddMessaging<AnalyticsDbContext>(builder.Configuration, "analytics", bus =>
{
    // Egyszerre egy üzenet: így ugyanannak a futónak két eseménye nem számolja újra párhuzamosan
    // (és egymást felülírva) a napi pontszámot.
    bus.AddConsumer<TrainingEventsConsumer>().Endpoint(endpoint => endpoint.ConcurrentMessageLimit = 1);
    bus.AddConsumer<CoachRunnerLinkConsumer>();
});
builder.Services.AddScoped<IRiskService, RiskService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddHostedService<DailyRiskJob>();

var app = builder.Build();

app.UseServiceDefaults();
app.MapRiskEndpoints();
app.MapAlertEndpoints();
app.MapRecommendationEndpoints();

await app.MigrateDatabaseAsync<AnalyticsDbContext>();
await app.RunAsync();
