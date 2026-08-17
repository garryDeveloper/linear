using Microsoft.Extensions.Options;

namespace Linear.Web.Shared.Http;

public static class ApiClientServiceCollectionExtensions
{
    /// <summary>
    /// Registra el cliente HTTP con el que los componentes Blazor consumen los
    /// endpoints internos.
    /// </summary>
    /// <remarks>
    /// Se registra como cliente tipado —y no como <see cref="HttpClient"/> suelto—
    /// para tener un único punto donde insertar el <c>DelegatingHandler</c> que
    /// reenvíe la cookie de autenticación cuando se implemente la task 002.
    /// </remarks>
    public static IServiceCollection AddInternalApiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<ApiClientOptions>()
            .Bind(configuration.GetSection(ApiClientOptions.SectionName));

        services.AddSingleton<ApiBaseAddressProvider>();

        services.AddHttpClient<ApiClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ApiClientOptions>>().Value;

            httpClient.BaseAddress = serviceProvider
                .GetRequiredService<ApiBaseAddressProvider>()
                .GetBaseAddress();

            httpClient.Timeout = options.Timeout;
        });

        return services;
    }
}
