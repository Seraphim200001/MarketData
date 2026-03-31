using Grpc.Net.Client;
using MarketData.Client.Grpc.Configuration;
using MarketData.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MarketData.Client.Grpc;

public class MarketDataGrpcConnectionBuilder : IMarketDataGrpcConnectionilder
{
    private readonly GrpcSettings _settings;
    private readonly ILogger _logger;
    private readonly GrpcChannel _channel;
    private readonly MarketDataService.MarketDataServiceClient _client;

    public MarketDataGrpcConnectionBuilder(IOptions<GrpcSettings> grpcSettings, ILogger? logger = null)
        : this(grpcSettings.Value, logger)
    {
    }

    public MarketDataGrpcConnectionBuilder(GrpcSettings settings, ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _channel = GrpcChannel.ForAddress(settings.ServerUrl);
        _client = new MarketDataService.MarketDataServiceClient(_channel);
    }

    public GrpcChannel Channel { get => _channel; }

    public async Task InitializeAsync(int maxRetries = 5, int initialRetryDelayMs = 100, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("Initializing gRPC connection to {Address}", _channel.Target);

        var retryDelay = TimeSpan.FromMilliseconds(initialRetryDelayMs);
        Exception? lastException = null;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await _channel.ConnectAsync(ct);
                _logger.LogInformation("gRPC connection established successfully");
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (i < maxRetries - 1)
                {
                    _logger.LogWarning("gRPC connection attempt {Attempt} of {MaxRetries} failed: {Message}",
                        i + 1, maxRetries, ex.Message);

                    _logger.LogInformation("Waiting {Delay} before next retry", retryDelay);
                    await Task.Delay(retryDelay, ct);
                    retryDelay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * 1.5);
                }
            }
        }

        _logger.LogError(lastException, "Failed to establish gRPC connection after {MaxRetries} attempts", maxRetries);
        throw lastException ?? new TimeoutException($"Failed to establish gRPC connection after {maxRetries} attempts");
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
}
