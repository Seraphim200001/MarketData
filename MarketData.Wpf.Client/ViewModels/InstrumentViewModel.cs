using System.Windows;
using System.Windows.Input;
using FancyCandles;
using Grpc.Core;
using MarketData.Grpc;
using MarketData.Wpf.Client.FancyCandlesImplementations;
using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Client.Views;
using MarketData.Wpf.Shared;
using Microsoft.Extensions.Options;

namespace MarketData.Wpf.Client.ViewModels;

public class InstrumentViewModel : ViewModelBase
{
    private readonly MarketDataService.MarketDataServiceClient _grpcClient;
    private readonly IModelConfigService _modelConfigService;

    private CandleBuilder<double> _candleBuilder;
    private int _loadHistoryOnStartMinutes = 1440;
    private int _candlePrecision = 2;

    private string _price;
    private string _instrument;
    private string _timestamp;
    private CancellationTokenSource? _cancellationTokenSource;
    private CandlesSource _candles;
    private bool _isStreaming;

    public InstrumentViewModel(
        MarketDataService.MarketDataServiceClient grpcClient, 
        IModelConfigService modelConfigService,
        IOptions<CandleChartSettings> candleChartConfig,
        string instrumentName)
    {
        _grpcClient = grpcClient;
        _modelConfigService = modelConfigService;
        _instrument = instrumentName;
        Price = "#.##";
        Timestamp = string.Empty;

        ModelConfigCommand = new AsyncRelayCommand(OpenModelConfigAsync);

        InitializeCandleChart(candleChartConfig);
    }

    private void InitializeCandleChart(IOptions<CandleChartSettings> candleChartConfig)
    {
        CandleChartSettings config;
        try
        {
            config = candleChartConfig.Value;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Invalid candle chart configuration: {ex.Message}", "Configuration Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }

        TimeFrame chartTimeFrame = config.CandleTimeFrame;
        _candlePrecision = config.CandlePrecision;
        _loadHistoryOnStartMinutes = config.LoadHistoryOnStartMinutes;
        _candleBuilder = new CandleBuilder<double>(
            TimeSpan.FromSeconds(chartTimeFrame.ToSeconds()), true);
        Candles = new CandlesSource(chartTimeFrame);
    }

    private async Task OpenModelConfigAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // timeout for loading config
        try
        {
            var config = await _modelConfigService.GetConfigurationsAsync(_instrument, cts.Token);
            var supportedModels = await _modelConfigService.GetSupportedModelsAsync(cts.Token);
            var vm = new ModelConfigViewModel(_instrument, _modelConfigService, config, supportedModels);
            var view = new ModelConfigWindow(vm);
            view.Show();
        }
        catch (OperationCanceledException oce)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show($"Loading configuration was cancelled: {oce.Message}", "Timeout",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show($"Failed to load configuration: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    public ICommand ModelConfigCommand { get; }

    public string Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public string Instrument
    {
        get => _instrument;
        set => SetProperty(ref _instrument, value);
    }

    public string Timestamp
    {
        get => _timestamp;
        set => SetProperty(ref _timestamp, value);
    }

    public CandlesSource Candles
    {
        get => _candles;
        set => SetProperty(ref _candles, value);
    }

    public bool IsStreaming
    {
        get => _isStreaming;
        private set => SetProperty(ref _isStreaming, value);
    }

    public async Task StartStreamingAsync()
    {
        if (IsStreaming)
            return; // is this ever hit?

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await GetHistoricalCandles(_cancellationTokenSource.Token)
                .WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show("Historical data could not be loaded within the timeout period. " +
                    "Streaming will continue with live data only.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show($"Failed to load historical data: {ex.Message}. " +
                    $"Streaming will continue with live data only.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        IsStreaming = true;

        try
        {
            var request = new SubscribeRequest();
            request.Instruments.Add(Instrument);

            using var call = _grpcClient.SubscribeToPrices(request, cancellationToken: _cancellationTokenSource.Token);

            await foreach (var priceUpdate in call.ResponseStream.ReadAllAsync(_cancellationTokenSource.Token))
            {
                // Update UI on the UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Price = priceUpdate.Value.ToString("F2");
                    Timestamp = new DateTime(priceUpdate.Timestamp)
                        .ToString("HH:mm:ss.fff");
                });

                await UpdateCandleChartAsync(priceUpdate);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            // Stream was cancelled, this is expected on shutdown
        }
        catch (Exception ex)
        {
            // Handle other errors
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Price = $"Error: {ex.Message}";
            });
        }
        finally
        {
            IsStreaming = false;
        }
    }

    private async Task GetHistoricalCandles(CancellationToken ct)
    {
        // load last N minutes to pre-populate the chart
        var now = DateTime.UtcNow;
        var start = now.AddMinutes(-_loadHistoryOnStartMinutes);
        var historicalData = _grpcClient.GetHistoricalData(new HistoricalDataRequest
        {
            Instrument = Instrument,
            StartTimestamp = start.Ticks,
            EndTimestamp = now.Ticks
        }, cancellationToken: ct);

        PriceUpdate? lastPrice = null;
        foreach (var dataPoint in historicalData.Prices.OrderBy(x => x.Timestamp))
        {
            await UpdateCandleChartAsync(dataPoint);
            lastPrice = dataPoint;
        }

        if (lastPrice != null)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Price = lastPrice.Value.ToString("F2");
                Timestamp = new DateTime(lastPrice.Timestamp)
                    .ToString("HH:mm:ss.fff");
            });
        }
    }

    private async Task UpdateCandleChartAsync(PriceUpdate priceUpdate)
    {
        var candle = _candleBuilder.AddPoint(
                            new DateTime(priceUpdate.Timestamp), priceUpdate.Value);

        if (candle is not null)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Candles.Add(new Candle(new DateTime(priceUpdate.Timestamp),
                    candle.Value, _candlePrecision));
            });
        }
    }

    public async Task StopStreamingAsync()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
        IsStreaming = false;
        await Task.CompletedTask;
    }
}
