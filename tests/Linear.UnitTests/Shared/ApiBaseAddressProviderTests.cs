using Linear.Web.Shared.Http;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Linear.UnitTests.Shared;

public class ApiBaseAddressProviderTests
{
    [Fact]
    public void ConfiguredAddress_TakesPrecedenceOverTheServerAddresses()
    {
        var provider = CreateProvider(
            configuredAddress: "https://linear.example.com/",
            serverAddresses: ["http://localhost:5000"]);

        Assert.Equal(new Uri("https://linear.example.com/"), provider.GetBaseAddress());
    }

    [Fact]
    public void AddressWithoutTrailingSlash_GetsOne()
    {
        // Sin la barra final, Uri descarta el último segmento al combinar rutas relativas.
        var provider = CreateProvider(configuredAddress: "https://linear.example.com");

        Assert.Equal("https://linear.example.com/", provider.GetBaseAddress().ToString());
    }

    [Fact]
    public void WithoutConfiguration_TheHttpsAddressOfTheServerIsPreferred()
    {
        var provider = CreateProvider(
            configuredAddress: null,
            serverAddresses: ["http://localhost:5173", "https://localhost:7262"]);

        Assert.Equal(new Uri("https://localhost:7262/"), provider.GetBaseAddress());
    }

    [Fact]
    public void WithoutHttps_TheHttpAddressIsUsed()
    {
        var provider = CreateProvider(
            configuredAddress: null,
            serverAddresses: ["http://localhost:5173"]);

        Assert.Equal(new Uri("http://localhost:5173/"), provider.GetBaseAddress());
    }

    [Theory]
    [InlineData("http://*:5000")]
    [InlineData("http://+:5000")]
    [InlineData("http://[::]:5000")]
    [InlineData("http://0.0.0.0:5000")]
    public void WildcardHosts_AreResolvedToLoopback(string serverAddress)
    {
        var provider = CreateProvider(configuredAddress: null, serverAddresses: [serverAddress]);

        Assert.Equal(new Uri("http://localhost:5000/"), provider.GetBaseAddress());
    }

    [Fact]
    public void WithoutConfigurationAndWithoutServerAddresses_TheFailureIsExplicit()
    {
        var provider = CreateProvider(configuredAddress: null, serverAddresses: []);

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetBaseAddress());

        Assert.Contains(nameof(ApiClientOptions.BaseAddress), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheAddressIsResolvedOnlyOnce()
    {
        var server = new FakeServer(["http://localhost:5173"]);
        var provider = new ApiBaseAddressProvider(server, Options.Create(new ApiClientOptions()));

        provider.GetBaseAddress();
        provider.GetBaseAddress();

        Assert.Equal(1, server.AddressesFeatureReads);
    }

    private static ApiBaseAddressProvider CreateProvider(
        string? configuredAddress = null,
        string[]? serverAddresses = null)
    {
        var options = Options.Create(new ApiClientOptions { BaseAddress = configuredAddress });

        return new ApiBaseAddressProvider(new FakeServer(serverAddresses ?? []), options);
    }

    /// <summary>
    /// Servidor mínimo que solo expone direcciones: alcanza para el contrato que
    /// <see cref="ApiBaseAddressProvider"/> consume.
    /// </summary>
    private sealed class FakeServer : IServer
    {
        private readonly CountingServerAddressesFeature _addresses;

        public FakeServer(string[] addresses)
        {
            _addresses = new CountingServerAddressesFeature(addresses);
            Features = new FeatureCollection();
            Features.Set<IServerAddressesFeature>(_addresses);
        }

        public IFeatureCollection Features { get; }

        public int AddressesFeatureReads => _addresses.Reads;

        public void Dispose()
        {
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
            where TContext : notnull => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class CountingServerAddressesFeature(string[] addresses) : IServerAddressesFeature
    {
        public int Reads { get; private set; }

        public ICollection<string> Addresses
        {
            get
            {
                Reads++;
                return addresses;
            }
        }

        public bool PreferHostingUrls { get; set; }
    }
}
