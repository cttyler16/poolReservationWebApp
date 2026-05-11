using PoolReservationWeb.Components;
using PoolReservationWeb.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<DatabaseHelper>();
builder.Services.AddSingleton<LoginRateLimiter>();
builder.Services.AddScoped<AdminSessionService>();
builder.Services.AddScoped<CustomerSessionService>();
builder.Services.AddScoped<ConfirmationState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
