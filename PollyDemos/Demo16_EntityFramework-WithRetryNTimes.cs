using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PollyDemos.EntityFramework;
using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// <para>
///     Demonstrates implementing an Entity Framework Core <see cref="IExecutionStrategy"/><br/>
///     that wraps executions within a <see cref="ResiliencePipeline"/> Execute method.<br/>
/// </para>
/// <para>
///     Observations:
///     <list type="bullet">
///         <item>There is a constant backoff between retries to give the database service some breathing room.</item>
///         <item>We limit the number of retries to 3 before ultimately failing. You may need to tweak the combination<br />
///         of delay backoff types (exponential vs. constant) and the max number of retry attempts for your environment.</item>
///     </list>
/// </para>
/// <para>
///     How to read the demo logs:
///     <list type="bullet">
///         <item>"Response: ... request #N(...)": Response received on time.</item>
///     </list>
/// </para>
/// </summary>
public class Demo16_EntityFramework_WithRetryNTimes : DemoBase
{
    public override string Description =>
        "This demo demonstrates using a Retry resilience pipeline with Entity Framework Core.";
    public int ItemsAddedToDatabase = 0;

    public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        EventualSuccesses = 0;
        EventualFailures = 0;
        TotalRequests = 0;

        PrintHeader(progress);

        var internalCancel = false;
        await using var context = new TodoDbContext();
        var strategy = context.Database.CreateExecutionStrategy();

        while (!(internalCancel || cancellationToken.IsCancellationRequested))
        {
            TotalRequests++;

            try
            {
                await strategy.ExecuteAsync(async token =>
                {
                    if (Random.Shared.NextDouble() > 0.8)
                    {
                        throw new InvalidOperationException("Simulating transient error!");
                    }

                    var todoItem = new TodoItem {Text = $"Todo Item - {TotalRequests}"};
                    context.Add(todoItem);
                    await context.SaveChangesAsync(cancellationToken);
                    progress.Report(ProgressWithMessage($"Inserted item into database : {todoItem.Text}", Color.Green));
                    EventualSuccesses++;
                    ItemsAddedToDatabase = context.TodoItems.Count();
                }, cancellationToken);
            }
            catch (Exception e)
            {
                progress.Report(ProgressWithMessage($"Request {TotalRequests} eventually failed with: {e.Message}",
                    Color.Red));
                EventualFailures++;
            }

            await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
            internalCancel = ShouldTerminateByKeyPress();
        }
    }

    public override Statistic[] LatestStatistics => new Statistic[]
    {
        new("Total requests made", TotalRequests),
        new("Requests which eventually succeeded", EventualSuccesses, Color.Green),
        new("Requests which eventually failed", EventualFailures, Color.Red),
        new("Items in database", ItemsAddedToDatabase, Color.White),
    };
}
