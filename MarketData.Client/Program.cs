using MarketData.Client;
using MarketData.Client.Shared.Configuration;
using Microsoft.Extensions.Configuration;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var grpcSettings = configuration.GetSection(GrpcSettings.SectionName)
            .Get<GrpcSettings>() ?? new GrpcSettings();

        var modelConfigClient = new GrpcModelConfigClient(grpcSettings);
        var availableInstruments = await modelConfigClient.GetConfiguredInstruments();

        var priceStreamer = new PriceStreamer(grpcSettings);
        await priceStreamer.Start();
    }
}