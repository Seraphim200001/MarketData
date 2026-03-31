using Grpc.Net.Client;
using MarketData.Client.Grpc.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MarketData.Client.Grpc;

public sealed class GrpcServicesBuilder
{
    private readonly IServiceCollection _services;
    private readonly Type _connectionType;

    internal GrpcServicesBuilder(IServiceCollection services, Type connectionType)
    {
        _services = services;
        _connectionType = connectionType;
    }

    /// <summary>
    /// Registers <typeparamref name="TImpl"/> as a singleton for <typeparamref name="TService"/>.
    /// The shared gRPC connection is automatically injected as the first constructor argument;
    /// all remaining parameters (e.g. ILogger&lt;TImpl&gt;) are resolved from DI.
    /// </summary>
    public GrpcServicesBuilder With<TService, TImpl>()
        where TService : class
        where TImpl : class, TService
    {
        _services.AddSingleton<TService>(sp =>
            ActivatorUtilities.CreateInstance<TImpl>(
                sp,
                sp.GetRequiredService(_connectionType)));
        return this;
    }
}

public static class GrpcServiceCollectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TImpl"/> as the <typeparamref name="TConnection"/> singleton,
    /// creating the gRPC channel from <see cref="IOptions{GrpcSettings}"/> already bound in DI.
    /// </summary>
    public static GrpcServicesBuilder AddGrpcConnections<TConnection, TImpl>(
        this IServiceCollection services,
        GrpcChannelOptions? channelOptions = null)
        where TConnection : class
        where TImpl : class, TConnection
    {
        services.AddSingleton<TConnection>(sp => channelOptions is null
            ? ActivatorUtilities.CreateInstance<TImpl>(sp,
                sp.GetRequiredService<IOptions<GrpcSettings>>())
            : ActivatorUtilities.CreateInstance<TImpl>(sp,
                sp.GetRequiredService<IOptions<GrpcSettings>>(),
                channelOptions));

        return new GrpcServicesBuilder(services, typeof(TConnection));
    }

    /// <summary>
    /// Registers <typeparamref name="TImpl"/> as the <typeparamref name="TConnection"/> singleton,
    /// using a plain <see cref="GrpcSettings"/> instance (e.g. for console / test scenarios
    /// where <see cref="IOptions{T}"/> is not bound).
    /// </summary>
    public static GrpcServicesBuilder AddGrpcConnections<TConnection, TImpl>(
        this IServiceCollection services,
        GrpcSettings settings,
        GrpcChannelOptions? channelOptions = null)
        where TConnection : class
        where TImpl : class, TConnection
    {
        services.AddSingleton<TConnection>(sp => channelOptions is null
            ? ActivatorUtilities.CreateInstance<TImpl>(sp, settings)
            : ActivatorUtilities.CreateInstance<TImpl>(sp, settings, channelOptions));

        return new GrpcServicesBuilder(services, typeof(TConnection));
    }
}
