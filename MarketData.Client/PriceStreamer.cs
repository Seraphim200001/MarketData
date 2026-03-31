using MarketData.Client.Shared.Services;

namespace MarketData.Client;

public class PriceStreamer
{
    private readonly IPriceService _priceService;

    public PriceStreamer(IPriceService priceService)
    {
        _priceService = priceService;
    }

    public async Task Start()
    {
        Console.Write("Enter instrument to stream: ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("No instrument specified. Exiting.");
            return;
        }

        var instrument = input.Trim();

        Console.WriteLine($"\nSubscribing to: {instrument}");
        Console.WriteLine("Waiting for price updates... (Press ESC to exit)\n");

        using var cts = new CancellationTokenSource();
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

        try
        {
            await foreach (var priceUpdate in _priceService.SubscribeToPricesAsync(instrument, cts.Token))
            {
                var timestamp = new DateTime(priceUpdate.Timestamp);
                Console.WriteLine($"[{timestamp:HH:mm:ss.fff}] {priceUpdate.Instrument,-10} {priceUpdate.Value:F4}");
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
}
