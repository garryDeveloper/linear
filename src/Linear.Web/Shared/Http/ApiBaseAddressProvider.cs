using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

namespace Linear.Web.Shared.Http;

/// <summary>
/// Resuelve la dirección base del API interno.
/// </summary>
/// <remarks>
/// Un componente Blazor Server corre dentro de un circuito, no dentro del
/// <c>HttpContext</c> del request original, así que no puede deducir la URL de la
/// aplicación desde la petición en curso. La dirección se toma de configuración y,
/// si no está, de las direcciones en las que Kestrel efectivamente escucha.
/// La resolución es perezosa porque el servidor todavía no tiene direcciones
/// asignadas mientras se construye el contenedor de dependencias.
/// </remarks>
public sealed class ApiBaseAddressProvider
{
    private readonly Lazy<Uri> _baseAddress;

    public ApiBaseAddressProvider(IServer server, IOptions<ApiClientOptions> options)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(options);

        _baseAddress = new Lazy<Uri>(
            () => Resolve(server, options.Value),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Uri GetBaseAddress() => _baseAddress.Value;

    private static Uri Resolve(IServer server, ApiClientOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.BaseAddress))
        {
            return new Uri(EnsureTrailingSlash(options.BaseAddress), UriKind.Absolute);
        }

        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;

        // Se prefiere HTTPS porque la aplicación redirige HTTP a HTTPS: apuntar el cliente
        // interno a la dirección HTTP provocaría un redirect en cada llamada.
        // En desarrollo esto requiere el certificado de desarrollo confiado
        // (`dotnet dev-certs https --trust`).
        var address =
            addresses?.FirstOrDefault(a => a.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            ?? addresses?.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException(
                $"No se pudo resolver la dirección base del API interno. " +
                $"Configurá '{ApiClientOptions.SectionName}:{nameof(ApiClientOptions.BaseAddress)}'.");
        }

        return new Uri(EnsureTrailingSlash(ReplaceWildcardHost(address)), UriKind.Absolute);
    }

    /// <summary>
    /// Kestrel puede reportar direcciones con host comodín (<c>http://*:5000</c>,
    /// <c>http://[::]:5000</c>) que no son navegables. Para una llamada a sí mismo,
    /// el equivalente correcto es el loopback.
    /// </summary>
    private static string ReplaceWildcardHost(string address) => address
        .Replace("://*:", "://localhost:", StringComparison.Ordinal)
        .Replace("://+:", "://localhost:", StringComparison.Ordinal)
        .Replace("://[::]:", "://localhost:", StringComparison.Ordinal)
        .Replace("://0.0.0.0:", "://localhost:", StringComparison.Ordinal);

    /// <summary>
    /// <see cref="Uri"/> descarta el último segmento de la base si no termina en barra,
    /// lo que rompería las URLs relativas del cliente.
    /// </summary>
    private static string EnsureTrailingSlash(string address) =>
        address.EndsWith('/') ? address : address + "/";
}
