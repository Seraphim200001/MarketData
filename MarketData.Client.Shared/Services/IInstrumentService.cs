using MarketData.Client.Shared.Models;

namespace MarketData.Client.Shared.Services;

/// <summary>
/// Service interface for managing instruments
/// </summary>
public interface IInstrumentService
{
    /// <summary>
    /// Gets all available instruments with their current configurations
    /// </summary>
    Task<IReadOnlyList<InstrumentConfig>> GetAllInstrumentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Attempts to add a new instrument
    /// </summary>
    Task<AddInstrumentResult> TryAddInstrumentAsync(string instrumentName, int tickIntervalMs, double initialPrice, CancellationToken ct = default);

    /// <summary>
    /// Attempts to remove an existing instrument
    /// </summary>
    Task<RemoveInstrumentResult> TryRemoveInstrumentAsync(string instrumentName, CancellationToken ct = default);
}
