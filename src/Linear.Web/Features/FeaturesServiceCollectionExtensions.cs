using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Linear.Web.Features;

public static class FeaturesServiceCollectionExtensions
{
    private const string HandlerSuffix = "Handler";

    /// <summary>
    /// Registra por convención todos los handlers de los vertical slices.
    /// </summary>
    /// <remarks>
    /// Cada operación de cada feature tiene su propio handler, así que registrarlos uno
    /// por uno convertiría <c>Program.cs</c> en una lista de decenas de líneas que hay que
    /// mantener a mano. La convención es: clase pública, no abstracta, dentro de
    /// <c>Linear.Web.Features</c>, con nombre terminado en <c>Handler</c>.
    /// El ciclo de vida es scoped porque los handlers dependen del <c>AppDbContext</c>.
    /// </remarks>
    public static IServiceCollection AddFeatureHandlers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var featuresNamespace = typeof(FeaturesServiceCollectionExtensions).Namespace!;

        var handlerTypes = typeof(FeaturesServiceCollectionExtensions).Assembly
            .GetExportedTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } &&
                type.Name.EndsWith(HandlerSuffix, StringComparison.Ordinal) &&
                type.Namespace?.StartsWith(featuresNamespace, StringComparison.Ordinal) == true);

        foreach (var handlerType in handlerTypes)
        {
            services.TryAddScoped(handlerType);
        }

        return services;
    }
}
