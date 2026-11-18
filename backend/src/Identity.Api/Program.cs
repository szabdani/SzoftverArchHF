using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Persistence;
using Identity.Api.Invitations;
using Identity.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddPostgresDbContext<IdentityDbContext>(builder.Configuration);
builder.Services.AddMessaging<IdentityDbContext>(builder.Configuration, "identity");
builder.Services.AddScoped<IInvitationService, InvitationService>();

var app = builder.Build();

app.UseServiceDefaults();
app.MapInvitationEndpoints();

await app.MigrateDatabaseAsync<IdentityDbContext>();
await app.RunAsync();
