namespace Linear.Web.Infrastructure.Realtime;

public static class RealtimeServiceCollectionExtensions
{
    /// <summary>
    /// Registra el tiempo real.
    /// </summary>
    /// <remarks>
    /// <see cref="ITeamNotifier"/> es singleton porque su registro de suscriptores tiene que
    /// sobrevivir a los circuitos: el aviso nace en la operación de un usuario y tiene que
    /// llegar a las pantallas de los demás, que viven en otros ámbitos.
    /// <para>
    /// El interceptor que produce los avisos se registra junto al resto de la persistencia,
    /// donde se fija su orden respecto del de actividad.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Blazor Server ya levanta SignalR para su circuito; declararlo igual deja escrito
        // que la aplicación lo necesita por sí misma y no de prestado.
        services.AddSignalR();

        services.AddSingleton<ITeamNotifier, TeamNotifier>();
        services.AddScoped<TeamRealtimeSubscriber>();

        return services;
    }
}
