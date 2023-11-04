using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Polly;

namespace PollyDemos.EntityFramework;

public class PollyExecutionStrategy : IExecutionStrategy
{
    private readonly ExecutionStrategyDependencies dependencies;
    private readonly ResiliencePipeline resiliencePipeline;

    public PollyExecutionStrategy(ExecutionStrategyDependencies dependencies, ResiliencePipeline resiliencePipeline)
    {
        this.dependencies = dependencies;
        this.resiliencePipeline = resiliencePipeline;
    }

    public bool RetriesOnFailure => true;

    public TResult Execute<TState, TResult>(
        TState state,
        Func<DbContext, TState, TResult> operation,
        Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
        => resiliencePipeline.Execute(() => operation(dependencies.CurrentContext.Context, state));

    public async Task<TResult> ExecuteAsync<TState, TResult>(
        TState state,
        Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
        Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
        CancellationToken cancellationToken = default)
        => await resiliencePipeline.ExecuteAsync(
            async (token) => await operation(dependencies.CurrentContext.Context, state, token),
            cancellationToken);
}
