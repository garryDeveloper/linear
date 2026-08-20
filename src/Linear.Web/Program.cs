using System.Text.Json.Serialization;

using FastEndpoints;

using Linear.Web.Components;
using Linear.Web.Components.Common;
using Linear.Web.Components.Theming;
using Linear.Web.Features;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Infrastructure.Realtime;

using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// UI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<SearchDialogLauncher>();

// API interna.
// El assembly se declara explícitamente porque bajo WebApplicationFactory el entry
// assembly es el proyecto de tests y la búsqueda automática no encontraría los endpoints.
builder.Services.AddFastEndpoints(options =>
{
    options.Assemblies = [typeof(FeaturesServiceCollectionExtensions).Assembly];
});
builder.Services.AddFeatureHandlers();

// Persistencia
builder.Services.AddPersistence(builder.Configuration, builder.Environment);
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<SampleDataSeeder>();

// Tiempo real
builder.Services.AddRealtime();

// Autenticación y autorización
builder.Services.AddAppAuthentication(builder.Environment, builder.Configuration);
builder.Services.AddScoped<ITeamAccess, TeamAccess>();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";

    // Los enums viajan por su nombre en las dos direcciones. Las respuestas ya exponen
    // los roles como texto ("Owner", "Admin"), así que aceptarlos solo como número
    // dejaría un contrato asimétrico y frágil ante cualquier reordenamiento del enum.
    config.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
});

// Los recursos estáticos se sirven a cualquiera: la página de login necesita sus hojas de
// estilo y su script antes de que exista una sesión, y la política de respaldo exige
// autenticación en todo endpoint que no diga lo contrario.
app.MapStaticAssets()
    .AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// El hub queda detrás de la autenticación por cookie, igual que el resto: una conexión
// anónima se rechaza en el handshake y nunca llega a pedir una suscripción.
app.MapHub<TeamHub>(TeamHub.Route);

await SeedDatabaseAsync(app);

await app.RunAsync();

static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var adminSeeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    var sampleSeeder = scope.ServiceProvider.GetRequiredService<SampleDataSeeder>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // El orden importa: los equipos de ejemplo incorporan la cuenta administradora,
        // así que esta tiene que existir antes.
        await adminSeeder.SeedAsync(CancellationToken.None);
        await sampleSeeder.SeedAsync(CancellationToken.None);
    }
    catch (Exception exception)
    {
        // Que falle la siembra no debe impedir que la aplicación arranque: sin base
        // disponible, la página de inicio tiene que poder informar el problema.
        logger.LogError(
            exception,
            "No se pudieron sembrar los datos iniciales. ¿Ejecutaste 'dotnet ef database update'?");
    }
}

/// <summary>
/// Declarada explícitamente para que los tests de integración puedan referenciarla
/// como punto de entrada de <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
