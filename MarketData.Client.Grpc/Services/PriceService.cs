using Grpc.Core;
using MarketData.Client.Shared.Services;
using MarketData.Grpc;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using SharedModels = MarketData.Client.Shared.Models;

namespace MarketData.Client.Grpc.Services;

public class PriceService : IPriceService
{
    private readonly ILogger<PriceService> _logger;
    private readonly MarketDataService.MarketDataServiceClient _client;

    public PriceService(IMarketDataGrpcConnectionBuilder grpcConnection, ILogger<PriceService> logger)
    {
        _logger = logger;
        _client = new MarketDataService.MarketDataServiceClient(grpcConnection.Channel);
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

        _logger.LogInformation("Received {Count} price updates for instrument {Instrument}",
            response.Prices.Count, instrument);

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

        _logger.LogInformation("Subscribed to price stream for instrument {Instrument}", instrument);

        await foreach (var update in call.ResponseStream.ReadAllAsync(ct))
        {
            yield return new SharedModels.PriceUpdate(update.Instrument, update.Value, update.Timestamp);
        }
    }
}
