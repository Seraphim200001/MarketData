using MarketData.Client.Shared.Models;

namespace MarketData.Client.Shared.Services;

/// <summary>
/// Service interface for streaming live prices and retrieving historical price data
/// </summary>
public interface IPriceService
{
    /// <summary>
    /// Asynchronously retrieves the most recent price update for the specified financial instrument.
    /// </summary>
    Task<PriceUpdate> GetLatestPriceAsync(string instrument, CancellationToken ct = default);

    /// <summary>
    /// Retrieves historical price data for a given instrument within a timestamp range
    /// </summary>
    Task<IReadOnlyList<PriceUpdate>> GetHistoricalDataAsync(
        string instrument, long startTimestamp, long endTimestamp, CancellationToken ct = default);

    /// <summary>
    /// Streams live price updates for an instrument
    /// </summary>
    IAsyncEnumerable<PriceUpdate> SubscribeToPricesAsync(string instrument, CancellationToken ct = default);
}
