using Grpc.Net.Client;

namespace MarketData.Client.Grpc;

public interface IMarketDataGrpcConnectionBuilder : IDisposable
{
    GrpcChannel Channel { get; }

    Task InitializeAsync(int maxRetries = 5, int initialRetryDelayMs = 100, CancellationToken ct = default);
}
