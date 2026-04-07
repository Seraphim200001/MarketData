using MarketData.Grpc;
using ConfigModels = MarketData.Client.Shared.Models;

namespace MarketData.Client.Grpc;

internal static class GrpcMappings
{
    internal static ConfigModels.InstrumentConfig ToInstrumentConfig(ConfigurationsResponse r) => new(
        r.InstrumentName,
        r.ActiveModel,
        r.TickIntervalMs,
        r.RandomMultiplicative != null
            ? new ConfigModels.RandomMultiplicativeConfig(r.RandomMultiplicative.StandardDeviation, r.RandomMultiplicative.Mean)
            : null,
        r.MeanReverting != null
            ? new ConfigModels.MeanRevertingConfig(r.MeanReverting.Mean, r.MeanReverting.Kappa, r.MeanReverting.Sigma, r.MeanReverting.Dt)
            : null,
        r.FlatConfigured,
        r.RandomAdditiveWalk != null
            ? new ConfigModels.RandomAdditiveWalkConfig(
                r.RandomAdditiveWalk.WalkSteps.Select(s => new ConfigModels.WalkStep(s.Probability, s.StepValue)).ToList())
            : null);
}
