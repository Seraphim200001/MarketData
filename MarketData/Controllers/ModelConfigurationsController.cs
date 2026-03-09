using MarketData.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketData.Controllers;

/// <summary>
/// API controller for managing instrument model configurations.
/// This is a thin controller that delegates all business logic to InstrumentModelManager.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModelConfigurationsController : ControllerBase
{
    private readonly IInstrumentModelManager _modelManager;

    public ModelConfigurationsController(IInstrumentModelManager modelManager)
    {
        _modelManager = modelManager;
    }

    /// <summary>
    /// Get the current active model and all available configurations for an instrument
    /// </summary>
    [HttpGet("{instrumentName}")]
    public async Task<ActionResult<object>> GetConfigurations(string instrumentName, CancellationToken ct)
    {
        var instrument = await _modelManager.GetInstrumentWithConfigurationsAsync(instrumentName, ct);

        if (instrument == null)
        {
            return NotFound($"Instrument '{instrumentName}' not found");
        }

        return Ok(new
        {
            instrument.Name,
            ActiveModel = instrument.ModelType,
            Configurations = new
            {
                RandomMultiplicative = instrument.RandomMultiplicativeConfig,
                MeanReverting = instrument.MeanRevertingConfig,
                Flat = instrument.FlatConfig != null ? new { Configured = true } : null,
                RandomAdditiveWalk = instrument.RandomAdditiveWalkConfig
            }
        });
    }

    /// <summary>
    /// Switch the active model for an instrument
    /// </summary>
    [HttpPut("{instrumentName}/model")]
    public async Task<ActionResult> SwitchModel(
        string instrumentName,
        [FromBody] SwitchModelRequest request,
        CancellationToken ct)
    {
        try
        {
            var previousModel = await _modelManager.SwitchModelAsync(instrumentName, request.ModelType, ct);
            return Ok(new
            {
                Message = "Model switched successfully. Changes are applied immediately.",
                PreviousModel = previousModel,
                NewModel = request.ModelType
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Update RandomMultiplicative configuration
    /// </summary>
    [HttpPut("{instrumentName}/config/random-multiplicative")]
    public async Task<ActionResult> UpdateRandomMultiplicativeConfig(
        string instrumentName,
        [FromBody] UpdateRandomMultiplicativeRequest request,
        CancellationToken ct)
    {
        try
        {
            var config = await _modelManager.UpdateRandomMultiplicativeConfigAsync(
                instrumentName,
                request.StandardDeviation,
                request.Mean,
                ct);

            return Ok(new
            {
                Message = "Configuration updated successfully. Changes are applied immediately.",
                Configuration = config
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Update MeanReverting configuration
    /// </summary>
    [HttpPut("{instrumentName}/config/mean-reverting")]
    public async Task<ActionResult> UpdateMeanRevertingConfig(
        string instrumentName,
        [FromBody] UpdateMeanRevertingRequest request,
        CancellationToken ct)
    {
        try
        {
            var config = await _modelManager.UpdateMeanRevertingConfigAsync(
                instrumentName,
                request.Mean,
                request.Kappa,
                request.Sigma,
                request.Dt,
                ct);

            return Ok(new
            {
                Message = "Configuration updated successfully. Changes are applied immediately.",
                Configuration = config
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }
}

public record SwitchModelRequest(string ModelType);

public record UpdateRandomMultiplicativeRequest(
    double StandardDeviation,
    double Mean);

public record UpdateMeanRevertingRequest(
    double Mean,
    double Kappa,
    double Sigma,
    double Dt);
