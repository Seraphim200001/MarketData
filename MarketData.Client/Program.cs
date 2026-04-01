using MarketData.Client;
using MarketData.Client.Grpc;
using MarketData.Client.Grpc.Configuration;
using MarketData.Client.Grpc.Services;
using MarketData.Client.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Text.Json;

/* TO BE DEPRECATED */

internal class Program
{
    private static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            LogBanner();
            Log.Information("Starting Console Market Data Client");

            var grpcSettings = configuration.GetSection(GrpcSettings.SectionName)
                .Get<GrpcSettings>() ?? new GrpcSettings();

            var services = new ServiceCollection();
            services.AddLogging(b => b.AddSerilog());
            services
                .AddGrpcConnections<IMarketDataGrpcConnectionBuilder, MarketDataGrpcConnectionBuilder>(grpcSettings)
                .With<IPriceService, PriceService>()
                .With<IInstrumentService, InstrumentService>()
                .With<IModelConfigService, ModelConfigService>();
            services.AddSingleton<PriceStreamer>();

            await using var sp = services.BuildServiceProvider();

            Log.Information("Initializing gRPC connections to {ServerUrl}", grpcSettings.ServerUrl);
            await sp.GetRequiredService<IMarketDataGrpcConnectionBuilder>().InitializeAsync();
            Log.Information("gRPC connections ready");

            var instrumentService = sp.GetRequiredService<IInstrumentService>();
            var priceStreamer = sp.GetRequiredService<PriceStreamer>();

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine($"Press (Ctrl+C) to exit.");
                Console.WriteLine();

                var availableInstruments = (await instrumentService.GetAllInstrumentsAsync())
                    .Select(c => c.InstrumentName);
                Console.WriteLine($"Available instruments: {string.Join(", ", availableInstruments)}");

                var sep = new string('=', 30);
                Console.WriteLine($"Menu {sep}");
                Console.WriteLine($"1. Add instrument");
                Console.WriteLine($"2. Remove instrument");
                Console.WriteLine($"3. View configurations");
                Console.WriteLine($"4. Start price streaming");

                Console.Write($">>> ");
                var input = Console.ReadLine();
                if (input == "1")
                {
                    Console.Write("Enter instrument name: ");
                    var name = Console.ReadLine();
                    if(string.IsNullOrWhiteSpace(name))
                    {
                        Console.WriteLine("Invalid instrument name");
                        continue;
                    }
                    Console.Write("Enter tick interval (ms): ");
                    if(!int.TryParse(Console.ReadLine(), out var tickIntervalMs))
                    {
                        Console.WriteLine("Invalid tick interval");
                        continue;
                    }
                    Console.Write("Enter initial price: ");
                    if(!double.TryParse(Console.ReadLine(), out var initialPrice))
                    {
                        Console.WriteLine("Invalid initial price");
                        continue;
                    }
                    await instrumentService.TryAddInstrumentAsync(name, tickIntervalMs, initialPrice);
                }
                else if (input == "2")
                {
                    Console.Write("Enter instrument name: ");
                    var name = Console.ReadLine();
                    if(string.IsNullOrWhiteSpace(name))
                    {
                        Console.WriteLine("Invalid instrument name");
                        continue;
                    }
                    await instrumentService.TryRemoveInstrumentAsync(name);
                }
                else if (input == "3")
                {
                    var res = await instrumentService.GetAllInstrumentsAsync();
                    var configs = JsonSerializer.Serialize(res);
                    Console.WriteLine(configs);
                }
                else if (input == "4")
                {
                    await priceStreamer.Start();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.Information("Shutting down Console Market Data Client");
            Log.CloseAndFlush();
        }
    }

    private static void LogBanner()
    {
        const string banner =
@"
          __  __            _        _       _       _        
         |  \/  |          | |      | |     | |     | |       
         | \  / | __ _ _ __| | _____| |_  __| | __ _| |_ __ _ 
         | |\/| |/ _` | '__| |/ / _ \ __|/ _` |/ _` | __/ _` |
         | |  | | (_| | |  |   <  __/ |_| (_| | (_| | || (_| |
         |_|  |_|\__,_|_|  |_|\_\___|\__|\__,_|\__,_|\__\__,_|
           ____                      _        
          / ___|___  _ __  ___  ___ | | ___   
         | |   / _ \| '_ \/ __|/ _ \| |/ _ \  
         | |__| (_) | | | \__ \ (_) | |  __/  
          \____\___/|_| |_|___/\___/|_|\___|  
                                              ";
        Log.Logger.Information(banner);
    }
}