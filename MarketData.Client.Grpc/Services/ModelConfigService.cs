using Grpc.Net.Client;
using MarketData.Client.Grpc.Configuration;
using MarketData.Client.Shared.Services;
using MarketData.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedModels = MarketData.Client.Shared.Models;

namespace MarketData.Client.Grpc.Services;

public class ModelConfigService : IModelConfigService, IDisposable
{
    private readonly ILogger<ModelConfigService> _logger;
    private readonly GrpcChannel _channel;
    private readonly ModelConfigurationService.ModelConfigurationServiceClient _client;

    private readonly IDisposable? _ownedChannel; // Track whether we own the channel to dispose it if necessary
    private bool _disposed;

    public ModelConfigService(IOptions<GrpcSettings> grpcSettings, ILogger<ModelConfigService> logger)
        : this(grpcSettings.Value, logger)
    {
    }

    public ModelConfigService(GrpcSettings grpcSettings, ILogger<ModelConfigService> logger)
    {
        _logger = logger;
        _channel = GrpcChannel.ForAddress(grpcSettings.ServerUrl);
        _ownedChannel = _channel;
        _client = new ModelConfigurationService.ModelConfigurationServiceClient(_channel);
    }

    public ModelConfigService(IMarketDataGrpcConnectionBuilder grpcConnection, ILogger<ModelConfigService> logger)
    {
        _logger = logger;
        _channel = grpcConnection.Channel;
        _client = new ModelConfigurationService.ModelConfigurationServiceClient(_channel);
    }

    public async Task<IReadOnlyList<string>> GetSupportedModelsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting supported models from gRPC service.");
        var response = await _client.GetSupportedModelsAsync(new GetSupportedModelsRequest(), cancellationToken: ct);
        return response.SupportedModels.ToList();
    }

    public async Task<SharedModels.InstrumentConfig> GetConfigurationsAsync(string instrumentName, CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting current configurations for instrument {Instrument} from gRPC service.", instrumentName);
        var response = await _client.GetConfigurationsAsync(
            new GetConfigurationsRequest { InstrumentName = instrumentName }, cancellationToken: ct);
        return GrpcMappings.ToInstrumentConfig(response);
    }

    public async Task<SharedModels.SwitchModelResult> SwitchModelAsync(string instrumentName, string modelType, CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting model switch for instrument {Instrument} to model type {ModelType} from gRPC service.", instrumentName, modelType);
        var response = await _client.SwitchModelAsync(new SwitchModelRequest
        {
            InstrumentName = instrumentName,
            ModelType = modelType
        }, cancellationToken: ct);
        return new SharedModels.SwitchModelResult(response.Message, response.PreviousModel, response.NewModel);
    }

    public async Task<SharedModels.UpdateConfigResult> UpdateTickIntervalAsync(string instrumentName, int tickIntervalMs, CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting tick interval update for instrument {Instrument} to {TickIntervalMs} ms from gRPC service.", instrumentName, tickIntervalMs);
        var response = await _client.UpdateTickIntervalAsync(new UpdateTickIntervalRequest
        {
            InstrumentName = instrumentName,
            TickIntervalMs = tickIntervalMs
        }, cancellationToken: ct);
        return new SharedModels.UpdateConfigResult(response.Success, response.Message);
    }

    public async Task UpdateRandomMultiplicativeConfigAsync(
        string instrumentName,
        double standardDeviation,
        double mean,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting Random Multiplicative config update for instrument {Instrument} " +
            "with StdDev={StandardDeviation}, Mean={Mean} from gRPC service.", instrumentName, standardDeviation, mean);

        await _client.UpdateRandomMultiplicativeConfigAsync(
            new UpdateRandomMultiplicativeRequest
            {
                InstrumentName = instrumentName,
                StandardDeviation = standardDeviation,
                Mean = mean
            }, cancellationToken: ct);
    }

    public async Task UpdateMeanRevertingConfigAsync(
        string instrumentName,
        double mean,
        double kappa,
        double sigma,
        double dt,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting Mean Reverting config update for instrument {Instrument} " +
            "with Mean={Mean}, Kappa={Kappa}, Sigma={Sigma}, Dt={Dt} from gRPC service.",
            instrumentName, mean, kappa, sigma, dt);

        await _client.UpdateMeanRevertingConfigAsync(
            new UpdateMeanRevertingRequest
            {
                InstrumentName = instrumentName,
                Mean = mean,
                Kappa = kappa,
                Sigma = sigma,
                Dt = dt
            }, cancellationToken: ct);
    }

    public async Task UpdateRandomAdditiveWalkConfigAsync(
        string instrumentName,
        IEnumerable<SharedModels.WalkStep> walkSteps,
        CancellationToken ct = default)
    {
        var request = new UpdateRandomAdditiveWalkRequest
        {
            InstrumentName = instrumentName
        };

        foreach (var step in walkSteps)
        {
            request.WalkSteps.Add(new WalkStep
            {
                Probability = step.Probability,
                StepValue = step.StepValue
            });
        }

        _logger.LogInformation("Requesting Random Additive Walk config update for instrument {Instrument} with {StepCount} steps from gRPC service.",
            instrumentName, request.WalkSteps.Count);
        await _client.UpdateRandomAdditiveWalkConfigAsync(request, cancellationToken: ct);
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
                _ownedChannel?.Dispose();
            }
            _disposed = true;
        }
    }
}
