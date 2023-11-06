using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Polly;

namespace PollyDemos.EntityFramework;


/// <summary>
/// Represents a class that implements the <see cref="IExecutionStrategy"/> interface using Polly for resilience and transient-fault handling.
/// </summary>
public class PollyExecutionStrategy : IExecutionStrategy
{
    private readonly ExecutionStrategyDependencies dependencies;
    private readonly ResiliencePipeline resiliencePipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollyExecutionStrategy"/> class with the specified dependencies and resilience pipeline.
    /// </summary>
    /// <param name="dependencies">The dependencies required by the execution strategy.</param>
    /// <param name="resiliencePipeline">The resilience pipeline used by the execution strategy.</param>
    public PollyExecutionStrategy(ExecutionStrategyDependencies dependencies, ResiliencePipeline resiliencePipeline)
    {
        this.dependencies = dependencies;
        this.resiliencePipeline = resiliencePipeline;
    }

    /// <summary>
    /// Gets a value indicating whether the execution strategy should retry on failure.
    /// </summary>
    public bool RetriesOnFailure => true;

    /// <summary>
    /// Executes the specified operation synchronously.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <typeparam name="TResult">The type of the result returned by the operation.</typeparam>
    /// <param name="state">The state object.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="verifySucceeded">The function used to verify that the operation succeeded.</param>
    /// <returns>The result returned by the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    public TResult Execute<TState, TResult>(
        TState state,
        Func<DbContext, TState, TResult> operation,
        Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
    {
        var operationState = (operation, dependencies.CurrentContext.Context, state);

        static TResult StaticOperation((Func<DbContext, TState, TResult> op, DbContext ctx, TState st) opState)
            => opState.op(opState.ctx, opState.st);

        return resiliencePipeline.Execute(() => StaticOperation(operationState));
    }

    /// <summary>
    /// Executes the specified operation asynchronously.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <typeparam name="TResult">The type of the result returned by the operation.</typeparam>
    /// <param name="state">The state object.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="verifySucceeded">The function used to verify that the operation succeeded.</param>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    public async Task<TResult> ExecuteAsync<TState, TResult>(
        TState state,
        Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
        Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
        CancellationToken cancellationToken = default)
    {
        var operationState = (operation, dependencies.CurrentContext.Context, state, cancellationToken);

        static async ValueTask<TResult> StaticOperationAsync((Func<DbContext, TState, CancellationToken, Task<TResult>> op, DbContext ctx, TState st, CancellationToken ct) opState)
            => await opState.op(opState.ctx, opState.st, opState.ct);

        return await resiliencePipeline.ExecuteAsync(
            (token) => StaticOperationAsync(operationState),
            cancellationToken);
    }
}
