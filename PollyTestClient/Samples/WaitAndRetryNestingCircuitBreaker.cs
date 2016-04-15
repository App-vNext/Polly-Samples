using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polly.CircuitBreaker;

namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates using the WaitAndRetry policy and CircuitBreaker.
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// </summary>
    public static class WaitAndRetryNestingCircuitBreaker
    {
        public static void Execute()
        {
            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            var client = new WebClient();
            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailuresDueToCircuitBreaking = 0;
            int eventualFailuresForOtherReasons = 0;

            // Define our waitAndRetry policy: keep retrying with 200ms gaps.
            var waitAndRetryPolicy = Policy
                .Handle<Exception>(e => !(e is BrokenCircuitException)) // Exception filtering!  We don't retry if the inner circuit-breaker says it's had enough!
                .WaitAndRetryForever(
                attempt => TimeSpan.FromMilliseconds(200),
                (Exception, calculatedWaitDuration) =>
                {
                    // This is your new exception handler! 
                    // Tell the user what they've won!
                    Console.WriteLine("Policy logging: " + Exception.Message);
                    retries++;
                });

            // Define our CircuitBreaker policy: Break if the action fails more than 5 times in a row.
            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreaker(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(3)
                );


            int i = 0;
            // Do the following until a key is pressed
            while (!Console.KeyAvailable)
            {
                i++;
                Stopwatch watch = new Stopwatch();
                watch.Start();

                try
                {
                    // Retry the following call according to the policy - 3 times.
                    waitAndRetryPolicy.Execute(() =>
                    {
                        // This code is executed within the waitAndRetryPolicy 

                        string msg = circuitBreakerPolicy.Execute<String>(() => // Note how we can also Execute() a Func<TResult> and pass back the value.
                                {
                                    // This code is executed within the circuitBreakerPolicy 

                                    // Make a request and get a response
                                    return client.DownloadString(Configuration.WEB_API_ROOT + "/api/values/" + i.ToString());
                                });

                        watch.Stop();

                        // Display the response message on the console
                        Console.Write("Response : " + msg);
                        Console.WriteLine(" (after " + watch.ElapsedMilliseconds + "ms)");
                        
                        eventualSuccesses++;
                    });
                }
                catch (BrokenCircuitException b)
                {
                    watch.Stop();
                    Console.Write("Request " + i + " failed with: " + b.GetType().Name);
                    Console.WriteLine(" (after " + watch.ElapsedMilliseconds + "ms)");
                    eventualFailuresDueToCircuitBreaking++;
                }
                catch (Exception e)
                {
                    watch.Stop();
                    Console.Write("Request " + i + " eventually failed with: " + e.Message);
                    Console.WriteLine(" (after " + watch.ElapsedMilliseconds + "ms)");
                    eventualFailuresForOtherReasons++;
                }

                // Wait half second
                Thread.Sleep(500);
            }

            Console.WriteLine("");
            Console.WriteLine("Total requests made                      : " + i);
            Console.WriteLine("Requests which eventually succeeded      : " + eventualSuccesses);
            Console.WriteLine("Retries made to help achieve success     : " + retries);
            Console.WriteLine("Requests which failed with broken circuit: " + eventualFailuresDueToCircuitBreaking);
            Console.WriteLine("Requests which eventually failed         : " + eventualFailuresForOtherReasons);

        }
    }
}
