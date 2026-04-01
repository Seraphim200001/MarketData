using Grpc.Net.Client;
using MarketData.Client.Grpc.Configuration;
using MarketData.Client.Shared.Services;
using MarketData.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedModels = MarketData.Client.Shared.Models;

namespace MarketData.Client.Grpc.Services;

public class InstrumentService : IInstrumentService, IDisposable
{
    private readonly ILogger<InstrumentService> _logger;
    private readonly GrpcChannel _channel;
    private readonly ModelConfigurationService.ModelConfigurationServiceClient _client;

    private bool _disposed;

    public InstrumentService(IOptions<GrpcSettings> grpcSettings, ILogger<InstrumentService> logger)
        : this(grpcSettings.Value, logger)
    {
    }

    public InstrumentService(GrpcSettings grpcSettings, ILogger<InstrumentService> logger)
    {
        _logger = logger;
        _channel = GrpcChannel.ForAddress(grpcSettings.ServerUrl);
        _client = new ModelConfigurationService.ModelConfigurationServiceClient(_channel);
    }

    public InstrumentService(IMarketDataGrpcConnectionBuilder grpcConnection, ILogger<InstrumentService> logger)
    {
        _logger = logger;
        _channel = grpcConnection.Channel;
        _client = new ModelConfigurationService.ModelConfigurationServiceClient(_channel);
    }

    public async Task<IReadOnlyList<SharedModels.InstrumentConfig>> GetAllInstrumentsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting instruments from gRPC service.");
        var response = await _client.GetAllInstrumentsAsync(new GetAllInstrumentsRequest(), cancellationToken: ct);
        return response.Configurations.Select(GrpcMappings.ToInstrumentConfig).ToList();
    }

    public async Task<SharedModels.AddInstrumentResult> TryAddInstrumentAsync(string instrumentName,
        int tickIntervalMs, double initialPrice, CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting to add instrument {Instrument} with tick interval {TickInterval} ms and initial price {InitialPrice} from gRPC service.",
            instrumentName, tickIntervalMs, initialPrice);

        var response = await _client.TryAddInstrumentAsync(new TryAddInstrumentRequest
        {
            InstrumentName = instrumentName,
            TickIntervalMs = tickIntervalMs,
            InitialPriceValue = initialPrice,
            InitialPriceTimestamp = DateTime.UtcNow.Ticks
        }, cancellationToken: ct);

        return new SharedModels.AddInstrumentResult(response.Added, response.Message);
    }

    public async Task<SharedModels.RemoveInstrumentResult> TryRemoveInstrumentAsync(string instrumentName,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting to remove instrument {Instrument} from gRPC service.", instrumentName);
        var response = await _client.TryRemoveInstrumentAsync(new TryRemoveInstrumentRequest
        {
            InstrumentName = instrumentName
        }, cancellationToken: ct);

        return new SharedModels.RemoveInstrumentResult(response.Removed, response.Message);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _channel?.Dispose();
            }
            _disposed = true;
        }
    }
}
