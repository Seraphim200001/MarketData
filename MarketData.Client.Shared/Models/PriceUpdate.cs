namespace MarketData.Client.Shared.Models;

public record PriceUpdate(string Instrument, double Value, long Timestamp);
