using BuildingBlocks;
using BuildingBlocks.Messaging;
using BuildingBlocks.Persistence;
using Identity.Api.Admin;
using Identity.Api.Demo;
using Identity.Api.Invitations;
using Identity.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddPostgresDbContext<IdentityDbContext>(builder.Configuration);
builder.Services.AddMessaging<IdentityDbContext>(builder.Configuration, "identity");
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.Configure<KeycloakAdminOptions>(builder.Configuration.GetSection(KeycloakAdminOptions.SectionName));
builder.Services.AddHttpClient<KeycloakAdminClient>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection(DemoOptions.SectionName));
builder.Services.AddScoped<IDemoService, DemoService>();

var app = builder.Build();

app.UseServiceDefaults("identity");
app.MapInvitationEndpoints();
app.MapAdminUserEndpoints();
app.MapDemoEndpoints();

await app.MigrateDatabaseAsync<IdentityDbContext>();
await app.RunAsync();
