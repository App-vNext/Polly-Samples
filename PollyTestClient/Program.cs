using System;
using System.Threading;
using System.Threading.Tasks;

namespace PollyTestClient
{
    class Program
    {
        static void Main(string[] args)
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

            //Samples.Demo00_NoPolicy.Execute();
            //Samples.Demo01_RetryNTimes.Execute();
            //Samples.Demo02_WaitAndRetryNTimes.Execute();
            //Samples.Demo03_WaitAndRetryNTimes_WithEnoughRetries.Execute();
            //Samples.Demo04_WaitAndRetryForever.Execute();
            //Samples.Demo05_WaitAndRetryWithExponentialBackoff.Execute();
            //Samples.Demo06_WaitAndRetryNestingCircuitBreaker.Execute();
            //Samples.Demo07_WaitAndRetryNestingCircuitBreakerUsingPolicyWrap.Execute();
            //Samples.Demo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker.Execute();
            //Samples.Demo09_Wrap_Fallback_Timeout_WaitAndRetry.Execute();

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Asynchronous demos
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // These async demos use .Wait() (rather than await) with the async calls, only for the purposes of allowing the demos still to remain the primary execution thread and own the Console output.

            Samples.AsyncDemo00_NoPolicy.ExecuteAsync(cancellationToken).Wait();
            //Samples.AsyncDemo01_RetryNTimes.ExecuteAsync(cancellationToken).Wait();
            //Samples.AsyncDemo02_WaitAndRetryNTimes.ExecuteAsync(cancellationToken).Wait();
            //Samples.AsyncDemo03_WaitAndRetryNTimes_WithEnoughRetries.ExecuteAsync(cancellationToken).Wait();
            //Samples.AsyncDemo04_WaitAndRetryForever.ExecuteAsync(cancellationToken).Wait();
            //Samples.AsyncDemo05_WaitAndRetryWithExponentialBackoff.ExecuteAsync(cancellationToken).Wait();
            //Samples.AsyncDemo06_WaitAndRetryNestingCircuitBreaker.ExecuteAsync(cancellationToken).Wait(); 
            //Samples.AsyncDemo07_WaitAndRetryNestingCircuitBreakerUsingPolicyWrap.ExecuteAsync(cancellationToken).Wait();
            //Samples.AsyncDemo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker.ExecuteAsync(cancellationToken).Wait();
            //Samples.AsyncDemo09_Wrap_Fallback_Timeout_WaitAndRetry.ExecuteAsync(cancellationToken).Wait();

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Bulkhead demos
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            //Samples.BulkheadAsyncDemo00_NoBulkhead.ExecuteAsync(cancellationToken).Wait();
            //Samples.BulkheadAsyncDemo01_WithBulkheads.ExecuteAsync(cancellationToken).Wait();


            // Keep the console open.
            Console.ReadLine();
            cancellationTokenSource.Cancel();
            Console.ReadLine();
        }
    }
}
