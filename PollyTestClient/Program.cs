using PollyTestClient.Samples;

namespace PollyTestClient
{
    class Program
    {
        static void Main()
        {
            // Execute samples:

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Uncomment the samples you wish to run:
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Work through the demos in order, to discover features.
            // See <summary> at top of each demo class, for explanation.
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Synchronous demos
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            Demo00_NoStrategy.Execute();
            //Demo01_RetryNTimes.Execute();
            //Demo02_WaitAndRetryNTimes.Execute();
            //Demo03_WaitAndRetryNTimes_WithEnoughRetries.Execute();
            //Demo04_WaitAndRetryForever.Execute();
            //Demo05_WaitAndRetryWithExponentialBackoff.Execute();
            //Demo06_WaitAndRetryNestingCircuitBreaker.Execute();
            //Demo07_WaitAndRetryNestingCircuitBreakerUsingPipeline.Execute();
            //Demo08_Pipeline_Fallback_WaitAndRetry_CircuitBreaker.Execute();
            //Demo09_Pipeline_Fallback_Timeout_WaitAndRetry.Execute();

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Asynchronous demos
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            CancellationTokenSource cancellationTokenSource = new();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // These async demos use .Wait() (rather than await) with the async calls, only for the purposes of allowing the demos still to remain the primary execution thread and own the Console output.

            //AsyncDemo00_NoPolicy.ExecuteAsync(cancellationToken).Wait();
            //AsyncDemo01_RetryNTimes.ExecuteAsync(cancellationToken).Wait();
            //AsyncDemo02_WaitAndRetryNTimes.ExecuteAsync(cancellationToken).Wait();
            //AsyncDemo03_WaitAndRetryNTimes_WithEnoughRetries.ExecuteAsync(cancellationToken).Wait();
            //AsyncDemo04_WaitAndRetryForever.ExecuteAsync(cancellationToken).Wait();
            //AsyncDemo05_WaitAndRetryWithExponentialBackoff.ExecuteAsync(cancellationToken).Wait();
            //AsyncDemo06_WaitAndRetryNestingCircuitBreaker.ExecuteAsync(cancellationToken).Wait();
            //AsyncDemo07_WaitAndRetryNestingCircuitBreakerUsingPolicyWrap.ExecuteAsync(cancellationToken).Wait();
            //AsyncDemo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker.ExecuteAsync(cancellationToken).Wait();
            //AsyncDemo09_Wrap_Fallback_Timeout_WaitAndRetry.ExecuteAsync(cancellationToken).Wait();

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Bulkhead demos
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            //BulkheadAsyncDemo00_NoBulkhead.ExecuteAsync(cancellationToken).Wait();
            //BulkheadAsyncDemo01_WithBulkheads.ExecuteAsync(cancellationToken).Wait();


            // Keep the console open.
            Console.ReadLine();
            cancellationTokenSource.Cancel();
            Console.ReadLine();
        }
    }
}
