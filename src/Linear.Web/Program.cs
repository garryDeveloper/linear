using FastEndpoints;

using Linear.Web.Components;
using Linear.Web.Components.Theming;
using Linear.Web.Features;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Http;

using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// UI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddScoped<ThemeState>();

// API interna.
// El assembly se declara explícitamente porque bajo WebApplicationFactory el entry
// assembly es el proyecto de tests y la búsqueda automática no encontraría los endpoints.
builder.Services.AddFastEndpoints(options =>
{
    options.Assemblies = [typeof(FeaturesServiceCollectionExtensions).Assembly];
});
builder.Services.AddFeatureHandlers();
builder.Services.AddInternalApiClient(builder.Configuration);

// Persistencia
builder.Services.AddPersistence(builder.Configuration, builder.Environment);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Las páginas de error son HTML y solo tienen sentido para la UI: si el API devolviera
// una página de error en lugar de un cuerpo JSON, el cliente no podría interpretarla.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>
/// Declarada explícitamente para que los tests de integración puedan referenciarla
/// como punto de entrada de <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
