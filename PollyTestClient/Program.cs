using System;

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


            Samples.Demo00_NoPolicy.Execute();
            //Samples.Demo01_RetryNTimes.Execute();
            //Samples.Demo02_WaitAndRetryNTimes.Execute();
            //Samples.Demo03_WaitAndRetryNTimes_WithEnoughRetries.Execute();
            //Samples.Demo04_WaitAndRetryForever.Execute();
            //Samples.Demo05_WaitAndRetryWithExponentialBackoff.Execute();
            //Samples.Demo06_WaitAndRetryNestingCircuitBreaker.Execute();


            //Task.Run(async () => { await Samples.Deprecated_NeedToUpdateToDemoStyle_RetryForeverAsync.Execute(); });

            // Keep the console open.
            Console.ReadLine();
        }
    }
}
