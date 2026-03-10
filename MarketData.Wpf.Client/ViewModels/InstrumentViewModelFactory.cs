using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using Microsoft.Extensions.Options;

namespace MarketData.Wpf.Client.ViewModels;

public class InstrumentViewModelFactory
{
    private readonly MarketDataService.MarketDataServiceClient _grpcClient;
    private readonly IModelConfigService _modelConfigService;
    private readonly IOptions<CandleChartSettings> _options;

    public InstrumentViewModelFactory(
        MarketDataService.MarketDataServiceClient grpcClient,
        IModelConfigService modelConfigService,
        IOptions<CandleChartSettings> options)
    {
        _grpcClient = grpcClient;
        _modelConfigService = modelConfigService;
        _options = options;
    }

    public InstrumentViewModel Create(string instrumentName)
    {
        return new InstrumentViewModel(_grpcClient, _modelConfigService, _options, instrumentName);
    }
}
