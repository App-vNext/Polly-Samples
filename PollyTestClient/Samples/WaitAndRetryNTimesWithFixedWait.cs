using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates using the WaitAndRetry policy.
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// </summary>
    public static class WaitAndRetryNTimesWithFixedWait
    {
        public static void Execute()
        {
            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            var client = new WebClient();
            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailures = 0;
            // Define our policy:
            var policy = Policy.Handle<Exception>().WaitAndRetry(3, 
                attempt => TimeSpan.FromMilliseconds(200),
                (Exception, calculatedWaitDuration) =>
            {
                // This is your new exception handler! 
                // Tell the user what they've won!
                Console.WriteLine("Exception: " + Exception.Message);
                retries++;

                // Wait 1/5 second before next retry.  (Added manually within RetryForever() demo; but Polly has overload to do this automatically...)
                // Thread.Sleep(200); // Commented out now: WaitAndRetry(...) Policy used above, to do this.
            });

            int i = 0;
            // Do the following until a key is pressed
            while (!Console.KeyAvailable)
            {
                i++;

                try
                {
                    // Retry the following call according to the policy - 3 times.
                    policy.Execute(() =>
                    {
                        // This code is executed within the Policy 

                        // Make a request and get a response
                        var msg = client.DownloadString(Configuration.WEB_API_ROOT + "/api/values/" + i.ToString());

                        // Display the response message on the console
                        Console.WriteLine("Response : " + msg);
                        eventualSuccesses++;
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine("Request " + i + " eventually failed with: " + e.Message);
                    eventualFailures++;
                }

                // Wait half second
                Thread.Sleep(500);
            }

            Console.WriteLine("");
            Console.WriteLine("Total requests made                 : " + i);
            Console.WriteLine("Requests which eventually succeeded : " + eventualSuccesses);
            Console.WriteLine("Retries made to help achieve success: " + retries);
            Console.WriteLine("Requests which eventually failed    : " + eventualFailures);

        }
    }
}
