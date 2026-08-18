using Linear.Domain.Users;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace Linear.Web.Infrastructure.Authentication;

public static class AppAuthenticationExtensions
{
    public const string AuthenticationCookieName = "linear.auth";

    private const string ApiPathPrefix = "/api";

    /// <summary>Fuerza (o no) que la cookie de sesión viaje solo por HTTPS.</summary>
    public const string RequireHttpsConfigurationKey = "Authentication:RequireHttps";

    /// <summary>Duración de una sesión recordada.</summary>
    public static readonly TimeSpan PersistentSessionLifetime = TimeSpan.FromDays(14);

    /// <summary>
    /// Configura autenticación por cookie y las políticas de autorización.
    /// </summary>
    /// <remarks>
    /// Se usa el manejador de cookies del framework en lugar de ASP.NET Core Identity
    /// completo: la pertenencia y los permisos por equipo se modelan con <c>TeamMember</c>,
    /// así que de las ocho tablas que crea Identity solo se aprovecharía una, y su tipo de
    /// usuario obligaría a que el dominio dependiera de la infraestructura de persistencia.
    /// El hash de contraseñas sí se toma del framework (ver <see cref="AspNetPasswordHasher"/>).
    /// </remarks>
    public static IServiceCollection AddAppAuthentication(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);

        var requireHttps = configuration.GetValue<bool?>(RequireHttpsConfigurationKey)
            ?? !environment.IsDevelopment();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = AuthenticationCookieName;
                options.Cookie.HttpOnly = true;

                // Lax y no Strict: el login vuelve por una redirección y con Strict el
                // navegador no mandaría la cookie recién emitida en ese primer request.
                options.Cookie.SameSite = SameSiteMode.Lax;


                // Exigir HTTPS en la cookie es lo correcto en cualquier despliegue real,
                // pero deja fuera los escenarios que corren sobre HTTP: el perfil de
                // desarrollo y los tests de integración en memoria. Por eso es una decisión
                // de configuración con un valor por omisión seguro.
                options.Cookie.SecurePolicy = requireHttps
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;

                options.ExpireTimeSpan = PersistentSessionLifetime;
                options.SlidingExpiration = true;

                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/account/access-denied";
                options.ReturnUrlParameter = "returnUrl";


                options.Events.OnValidatePrincipal = CookieValidator.ValidateAsync;

                // Sin sesión, el manejador de cookies redirige al login. Para el API eso es
                // una respuesta inservible: un cliente que espera JSON recibiría el HTML de
                // la pantalla de login con un 200. Bajo /api se responde con el código real.
                options.Events.OnRedirectToLogin = context =>
                    RespondWithStatusCodeForApi(context, StatusCodes.Status401Unauthorized);

                options.Events.OnRedirectToAccessDenied = context =>
                    RespondWithStatusCodeForApi(context, StatusCodes.Status403Forbidden);
            });

        services
            .AddAuthorizationBuilder()

            // Todo endpoint sin metadata de autorización queda protegido por omisión.
            // Abrir el acceso pasa a ser una decisión explícita: [AllowAnonymous].
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())

            .AddPolicy(AuthorizationPolicies.RequireAdmin, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(nameof(UserRole.Admin)))

            .AddPolicy(AuthorizationPolicies.RequireMember, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(nameof(UserRole.Admin), nameof(UserRole.Member)));

        services.AddCascadingAuthenticationState();

        return services;
    }

    private static Task RespondWithStatusCodeForApi(
        RedirectContext<CookieAuthenticationOptions> context,
        int statusCode)
    {
        if (context.Request.Path.StartsWithSegments(ApiPathPrefix))
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}
