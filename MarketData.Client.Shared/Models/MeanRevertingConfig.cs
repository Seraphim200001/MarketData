namespace MarketData.Client.Shared.Models;

public record MeanRevertingConfig(double Mean, double Kappa, double Sigma, double Dt);
