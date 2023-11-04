using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;

namespace PollyDemos.EntityFramework;
/// <summary>
/// Represents a factory for creating <see cref="PollyExecutionStrategy"/> instances.
/// </summary>
public class PollyExecutionStrategyFactory : IExecutionStrategyFactory
{
    private readonly ExecutionStrategyDependencies dependencies;
    private readonly ResiliencePipeline resiliencePipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollyExecutionStrategyFactory"/> class.
    /// </summary>
    /// <param name="dependencies">The dependencies.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public PollyExecutionStrategyFactory(ExecutionStrategyDependencies dependencies, ILoggerFactory loggerFactory)
    {
        this.dependencies = dependencies;
        resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                BackoffType = DelayBackoffType.Constant,
                MaxRetryAttempts = 3,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is InvalidOperationException
                    or TimeoutRejectedException)
            })
            .AddTimeout(TimeSpan.FromSeconds(1))
            .ConfigureTelemetry(loggerFactory)
            .Build();
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PollyExecutionStrategy"/> class.
    /// </summary>
    public IExecutionStrategy Create() => new PollyExecutionStrategy(dependencies, resiliencePipeline);
}
