namespace MarketData.Client.Shared.Models;

public record InstrumentConfig(
    string InstrumentName,
    string ActiveModel,
    int TickIntervalMs,
    RandomMultiplicativeConfig? RandomMultiplicative,
    MeanRevertingConfig? MeanReverting,
    bool FlatConfigured,
    RandomAdditiveWalkConfig? RandomAdditiveWalk);
