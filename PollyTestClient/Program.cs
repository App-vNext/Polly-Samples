using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Polly;

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


            //Samples.RetryNTimes.Execute();
            //Samples.RetryForever.Execute();
            //Samples.WaitAndRetryNTimesWithFixedWait.Execute();
            //Samples.WaitAndRetryNTimesWithExponentialBackoff.Execute();
            Samples.WaitAndRetryNestingCircuitBreaker.Execute();
            
            //Task.Run(async () => { await Samples.RetryForeverAsync.Execute(); });

            // Keep the console open.
            Console.ReadLine();
        }
    }
}
