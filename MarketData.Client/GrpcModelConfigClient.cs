using MarketData.Client.Shared.Configuration;
using MarketData.Grpc;
using System.Text.Json;

namespace MarketData.Client;

internal class GrpcModelConfigClient : GrpcClientBase
{
    private readonly ModelConfigurationService.ModelConfigurationServiceClient _client;
    public GrpcModelConfigClient(GrpcSettings settings) : base(settings)
    {
        _client = new ModelConfigurationService.ModelConfigurationServiceClient(_channel);
    }

    public async Task<IEnumerable<string>> GetConfiguredInstruments()
    {
        var result = await _client.GetAllInstrumentsAsync(new GetAllInstrumentsRequest());
        result.Configurations.ToList().ForEach(config =>
        {
            Console.WriteLine($"Instrument: {config.InstrumentName}");
            var configAsString = JsonSerializer.Serialize(config);
            Console.WriteLine("Config:");
            Console.WriteLine(configAsString);
        });

        return result.Configurations.Select(c => c.InstrumentName);
    }
}
