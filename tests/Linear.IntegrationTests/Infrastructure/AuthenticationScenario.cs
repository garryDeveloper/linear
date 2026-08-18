using System.Net;
using System.Text.RegularExpressions;

using Linear.Domain.Users;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace Linear.IntegrationTests.Infrastructure;

/// <summary>
/// Utilidades para armar escenarios de sesión en los tests.
/// </summary>
/// <remarks>
/// El inicio de sesión se hace enviando el formulario real de la pantalla de login, con su
/// token antiforgery: es el mismo camino que recorre un navegador, así que el test cubre
/// también el renderizado estático y la emisión de la cookie.
/// </remarks>
internal static partial class AuthenticationScenario
{
    public const string DefaultPassword = "Linear-Test-1234";

    public static async Task<User> CreateUserAsync(
        DatabaseWebApplicationFactory factory,
        string email,
        UserRole role = UserRole.Member,
        string password = DefaultPassword,
        bool isActive = true,
        string name = "Usuario de prueba")
    {
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var now = DateTimeOffset.UtcNow;
        var user = User.Create(Email.Create(email).Value, name, role, "pendiente", now).Value;

        user.ChangePasswordHash(passwordHasher.Hash(user, password), now);

        if (!isActive)
        {
            user.Deactivate(now);
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

    public static async Task DeactivateUserAsync(DatabaseWebApplicationFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await dbContext.Users.FirstAsync(candidate => candidate.Id == userId);

        user.Deactivate(DateTimeOffset.UtcNow);

        await dbContext.SaveChangesAsync();
    }

    public static HttpClient CreateClient(DatabaseWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Envía el formulario de inicio de sesión y devuelve la respuesta cruda.</summary>
    public static async Task<HttpResponseMessage> PostLoginAsync(
        HttpClient client,
        string email,
        string password,
        bool rememberMe = true)
    {
        var loginPage = await client.GetStringAsync("/account/login");

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "login",
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(loginPage),
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = rememberMe.ToString()
        });

        return await client.PostAsync("/account/login", form);
    }

    /// <summary>Devuelve un cliente con la sesión ya iniciada.</summary>
    public static async Task<HttpClient> SignInAsync(
        DatabaseWebApplicationFactory factory,
        string email,
        string password = DefaultPassword)
    {
        var client = CreateClient(factory);

        using var response = await PostLoginAsync(client, email, password);

        if (!HasSessionCookie(response))
        {
            throw new InvalidOperationException($"No se pudo iniciar sesión como '{email}'.");
        }

        return client;
    }


    /// <summary>Envía el formulario de cierre de sesión.</summary>
    public static async Task<HttpResponseMessage> PostLogoutAsync(HttpClient client)
    {
        var logoutPage = await client.GetStringAsync("/account/logout");

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "logout",
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(logoutPage)
        });

        return await client.PostAsync("/account/logout", form);
    }

    /// <summary>
    /// Cuerpo HTML de una respuesta, con las entidades ya decodificadas.
    /// </summary>
    /// <remarks>
    /// Razor codifica todo carácter fuera de ASCII, así que "contraseña" viaja como
    /// "contrase&#xF1;a". Comparar contra el texto original exige decodificar primero.
    /// </remarks>
    public static async Task<string> ReadHtmlAsync(HttpResponseMessage response) =>
        WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

    /// <summary>Ruta a la que apunta una redirección, sin el host.</summary>
    public static string RedirectPath(HttpResponseMessage response)
    {
        var location = response.Headers.Location;

        if (location is null)
        {
            return string.Empty;
        }

        return location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
    }
    public static bool HasSessionCookie(HttpResponseMessage response) =>
        SessionCookies(response).Any(cookie =>
            cookie.StartsWith($"{AppAuthenticationExtensions.AuthenticationCookieName}=", StringComparison.Ordinal) &&
            !cookie.StartsWith($"{AppAuthenticationExtensions.AuthenticationCookieName}=;", StringComparison.Ordinal));

    public static bool ClearsSessionCookie(HttpResponseMessage response) =>
        SessionCookies(response).Any(cookie =>
            cookie.StartsWith($"{AppAuthenticationExtensions.AuthenticationCookieName}=;", StringComparison.Ordinal));

    private static IEnumerable<string> SessionCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues(HeaderNames.SetCookie, out var cookies) ? cookies : [];

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenPattern().Match(html);

        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException("La pantalla de login no incluyó un token antiforgery.");
    }

    [GeneratedRegex("""name="__RequestVerificationToken" value="([^"]+)""")]
    private static partial Regex AntiforgeryTokenPattern();
}
