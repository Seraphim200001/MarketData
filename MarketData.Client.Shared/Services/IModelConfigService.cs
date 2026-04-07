using MarketData.Client.Shared.Models;

namespace MarketData.Client.Shared.Services;

/// <summary>
/// Service interface for managing model configurations
/// </summary>
public interface IModelConfigService
{
    /// <summary>
    /// Gets the list of supported model types
    /// </summary>
    Task<IReadOnlyList<string>> GetSupportedModelsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current configuration for a specific instrument
    /// </summary>
    Task<InstrumentConfig> GetConfigurationsAsync(string instrumentName, CancellationToken ct = default);

    /// <summary>
    /// Switches the active model for an instrument
    /// </summary>
    Task<SwitchModelResult> SwitchModelAsync(string instrumentName, string modelType, CancellationToken ct = default);

    /// <summary>
    /// Updates the tick interval for an instrument
    /// </summary>
    Task<UpdateConfigResult> UpdateTickIntervalAsync(string instrumentName, int tickIntervalMs, CancellationToken ct = default);

    /// <summary>
    /// Updates RandomMultiplicative model configuration
    /// </summary>
    Task UpdateRandomMultiplicativeConfigAsync(string instrumentName, double standardDeviation, double mean, CancellationToken ct = default);

    /// <summary>
    /// Updates MeanReverting model configuration
    /// </summary>
    Task UpdateMeanRevertingConfigAsync(string instrumentName, double mean, double kappa, double sigma, double dt, CancellationToken ct = default);

    /// <summary>
    /// Updates RandomAdditiveWalk model configuration
    /// </summary>
    Task UpdateRandomAdditiveWalkConfigAsync(string instrumentName, IEnumerable<WalkStep> walkSteps, CancellationToken ct = default);
}
