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
            // Synchonous demos
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            //Samples.Demo00_NoPolicy.Execute();
            //Samples.Demo01_RetryNTimes.Execute();
            //Samples.Demo02_WaitAndRetryNTimes.Execute();
            //Samples.Demo03_WaitAndRetryNTimes_WithEnoughRetries.Execute();
            //Samples.Demo04_WaitAndRetryForever.Execute();
            //Samples.Demo05_WaitAndRetryWithExponentialBackoff.Execute();
            //Samples.Demo06_WaitAndRetryNestingCircuitBreaker.Execute();


            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Asynchonous demos
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            //Task.Run(async () => { await Samples.AsyncDemo00_NoPolicy.Execute(cancellationToken); }); // This syntax works less well in the context of a ConsoleApp.  Control proceeds immediately to the ReadLine() below, which means the demos don't seem able to respond to Console.KeyAvailable, because ReadLine() is capturing them ...  The .Wait() syntax works better.

            Samples.AsyncDemo00_NoPolicy.Execute(cancellationToken).Wait();
            //Samples.AsyncDemo01_RetryNTimes.Execute(cancellationToken).Wait();
            //Samples.AsyncDemo02_WaitAndRetryNTimes.Execute(cancellationToken).Wait();
            //Samples.AsyncDemo03_WaitAndRetryNTimes_WithEnoughRetries.Execute(cancellationToken).Wait();
            //Samples.AsyncDemo04_WaitAndRetryForever.Execute(cancellationToken).Wait();
            //Samples.AsyncDemo05_WaitAndRetryWithExponentialBackoff.Execute(cancellationToken).Wait();
            //Samples.AsyncDemo06_WaitAndRetryNestingCircuitBreaker.Execute(cancellationToken).Wait(); 

            // Keep the console open.
            Console.ReadLine();
        }
    }
}
