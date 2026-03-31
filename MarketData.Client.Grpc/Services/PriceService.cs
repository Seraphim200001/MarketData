using Grpc.Core;
using Grpc.Net.Client;
using MarketData.Client.Grpc.Configuration;
using MarketData.Client.Shared.Services;
using MarketData.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using SharedModels = MarketData.Client.Shared.Models;

namespace MarketData.Client.Grpc.Services;

public class PriceService : IPriceService, IDisposable
{
    private readonly ILogger<PriceService> _logger;
    private readonly GrpcChannel _channel;
    private readonly MarketDataService.MarketDataServiceClient _client;

    private bool _disposed;

    public PriceService(IOptions<GrpcSettings> grpcSettings, ILogger<PriceService> logger)
        : this(grpcSettings.Value, logger)
    {
    }

    public PriceService(GrpcSettings grpcSettings, ILogger<PriceService> logger)
    {
        _logger = logger;
        _channel = GrpcChannel.ForAddress(grpcSettings.ServerUrl);
        _client = new MarketDataService.MarketDataServiceClient(_channel);
    }

    public PriceService(IMarketDataGrpcConnectionilder grpcConnection, ILogger<PriceService> logger)
    {
        _logger = logger;
        _channel = grpcConnection.Channel;
        _client = new MarketDataService.MarketDataServiceClient(_channel);
    }

    public async Task<IReadOnlyList<SharedModels.PriceUpdate>> GetHistoricalDataAsync(
        string instrument, long startTimestamp, long endTimestamp, CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting historical data for instrument {Instrument} from {Start} to {End}",
            instrument, new DateTime(startTimestamp, DateTimeKind.Utc), new DateTime(endTimestamp, DateTimeKind.Utc));

        var response = await _client.GetHistoricalDataAsync(new HistoricalDataRequest
        {
            Instrument = instrument,
            StartTimestamp = startTimestamp,
            EndTimestamp = endTimestamp
        }, cancellationToken: ct);

        return response.Prices
            .Select(p => new SharedModels.PriceUpdate(p.Instrument, p.Value, p.Timestamp))
            .ToList();
    }

    public async IAsyncEnumerable<SharedModels.PriceUpdate> SubscribeToPricesAsync(
        string instrument, [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogInformation("Subscribing to price stream for instrument {Instrument}", instrument);
        var request = new SubscribeRequest();
        request.Instruments.Add(instrument);

        using var call = _client.SubscribeToPrices(request, cancellationToken: ct);
        await foreach (var update in call.ResponseStream.ReadAllAsync(ct))
        {
            yield return new SharedModels.PriceUpdate(update.Instrument, update.Value, update.Timestamp);
        }
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

    //public async  Task<SharedModels.PriceUpdate?> GetLatestPriceAsync(string instrument, CancellationToken ct = default)
    //{
    //    var price = (await _client.GetHistoricalDataAsync(new HistoricalDataRequest { Instrument = instrument }))
    //        .OrderByDescending(p => p.Timestamp)
    //        .FirstOrDefault();

    //    return price;
    //}
}
