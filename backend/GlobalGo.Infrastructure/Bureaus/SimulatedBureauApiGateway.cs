using GlobalGo.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GlobalGo.Infrastructure.Bureaus;

public sealed class SimulatedBureauApiGateway : ISimulatedBureauApiGateway
{
    private readonly int _minLatencyMs;
    private readonly int _maxLatencyMs;
    private readonly int _maxRetries;
    private readonly int _retryBaseDelayMs;
    private readonly ILogger<SimulatedBureauApiGateway> _logger;

    public SimulatedBureauApiGateway(IConfiguration configuration, ILogger<SimulatedBureauApiGateway> logger)
    {
        _logger = logger;
        _minLatencyMs = ReadInt(configuration, "BureauApiSimulation:MinLatencyMs", 120);
        _maxLatencyMs = Math.Max(_minLatencyMs, ReadInt(configuration, "BureauApiSimulation:MaxLatencyMs", 350));
        _maxRetries = Math.Max(0, ReadInt(configuration, "BureauApiSimulation:MaxRetries", 2));
        _retryBaseDelayMs = Math.Max(10, ReadInt(configuration, "BureauApiSimulation:RetryBaseDelayMs", 120));
    }

    public async Task<TResponse> RequestAsync<TResponse>(
        string sourceCode,
        CreditSourceContext context,
        Func<int, TResponse> responseFactory,
        CancellationToken cancellationToken = default)
    {
        var seed = SimulatedBureauSeed.FromDni(context.Dni);
        var maxAttempts = _maxRetries + 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await SimulateLatencyAsync(cancellationToken);

            if (ShouldFailAttempt(seed, attempt) && attempt < maxAttempts)
            {
                _logger.LogWarning(
                    "Fuente externa simulada {SourceCode} falló en intento {Attempt}. Reintentando.",
                    sourceCode,
                    attempt);

                await Task.Delay(_retryBaseDelayMs * attempt, cancellationToken);
                continue;
            }

            return responseFactory(seed);
        }

        return responseFactory(seed);
    }

    private async Task SimulateLatencyAsync(CancellationToken cancellationToken)
    {
        var latency = Random.Shared.Next(_minLatencyMs, _maxLatencyMs + 1);
        await Task.Delay(latency, cancellationToken);
    }

    private static bool ShouldFailAttempt(int seed, int attempt)
    {
        if (attempt == 1)
        {
            return seed % 13 == 0;
        }

        if (attempt == 2)
        {
            return seed % 29 == 0;
        }

        return false;
    }

    private static int ReadInt(IConfiguration configuration, string key, int defaultValue)
        => int.TryParse(configuration[key], out var parsed) ? parsed : defaultValue;
}