using MarketData.Client.Shared.Services;
using Microsoft.Extensions.Logging;

namespace MarketData.Client;

public class PriceStreamer
{
    private readonly IPriceService _priceService;
    private readonly IInstrumentService _instrumentService;
    private readonly ILogger<PriceStreamer> _logger;

    public PriceStreamer(IPriceService priceService,
        IInstrumentService instrumentService,
        ILogger<PriceStreamer> logger)
    {
        _priceService = priceService;
        _instrumentService = instrumentService;
        _logger = logger;
    }

    public async Task Start()
    {
        using CancellationTokenSource cts = SetupCtsAndEscape();

        //pre-fetch available instruments to help user know what to input
        var instruments = (await _instrumentService.GetAllInstrumentsAsync()).Select(c => c.InstrumentName);

        if (!instruments.Any())
        {
            _logger.LogInformation("No instruments available to stream.");
            return;
        }

        var instrument = PromptInstrumentFromUser(instruments);

        _logger.LogInformation($"\nSubscribing to: {instrument}");
        _logger.LogInformation("Waiting for price updates... (Press ESC to exit)\n");

        try
        {
            await foreach (var priceUpdate in _priceService.SubscribeToPricesAsync(instrument, cts.Token))
            {
                var timestamp = new DateTime(priceUpdate.Timestamp);
                Console.WriteLine($"[{timestamp:s}] {priceUpdate.Instrument,-10} {priceUpdate.Value:F4}");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nShutting down...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
    }

    private static string PromptInstrumentFromUser(IEnumerable<string> availableInstruments)
    {
        string input = string.Empty;
        do
        {
            Console.Write("Available instruments: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(string.Join(", ", availableInstruments));
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Console.Write("Enter instrument to stream: ");
            var nullableInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(nullableInput))
            {
                Console.WriteLine("No instrument specified.");
                continue;
            }

            input = nullableInput.Trim();
            if (!availableInstruments.Contains(input, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Instrument '{input}' is not in the available list.");
            }
        } while (!availableInstruments.Contains(input, StringComparer.OrdinalIgnoreCase));

        return input;
    }

    private static CancellationTokenSource SetupCtsAndEscape()
    {
        var cts = new CancellationTokenSource();
        _ = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        cts.Cancel();
                        break;
                    }
                }
                Thread.Sleep(100);
            }
        });
        return cts;
    }
}
