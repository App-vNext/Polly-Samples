
using Polly;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates WaitAndRetry policy with calculated retry delays to back off.
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// 
    /// Observations: All calls still succeed!  Yay!
    /// But we didn't hammer the underlying server so hard - we backed off.
    /// That's healthier for it, if it might be struggling ...
    /// ... and if a lot of clients might be doing this simultaneously.
    /// 
    /// ... What if the underlying system was totally down tho?  
    /// ... Keeping trying forever would be counterproductive (so, see Demo06)
    /// </summary>
    public static class AsyncDemo05_WaitAndRetryWithExponentialBackoff
    {
        public static async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine(typeof(AsyncDemo05_WaitAndRetryWithExponentialBackoff).Name);
            Console.WriteLine("=======");

            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            var client = new HttpClient();
            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailures = 0;

            var policy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(6, // We can also do this with WaitAndRetryForever... but chose WaitAndRetry this time.
                attempt => TimeSpan.FromSeconds(0.1 * Math.Pow(2, attempt)), // Back off!  2, 4, 8, 16 etc times 1/4-second
                (exception, calculatedWaitDuration) =>  // Capture some info for logging!
                {
                    // This is your new exception handler! 
                    // Tell the user what they've won!
                    ConsoleHelper.WriteLineInColor("Exception: " + exception.Message, ConsoleColor.Yellow);
                    ConsoleHelper.WriteLineInColor(" ... automatically delaying for " + calculatedWaitDuration.TotalMilliseconds + "ms.", ConsoleColor.Yellow);
                    retries++;

                });
            int i = 0;
            // Do the following until a key is pressed
            while (!Console.KeyAvailable && !cancellationToken.IsCancellationRequested)
            {
                i++;

                try
                {
                    // Retry the following call according to the policy - 15 times.
                    await policy.ExecuteAsync(async token =>
                    {
                        // This code is executed within the Policy 

                        // Make a request and get a response
                        string msg = await client.GetStringAsync(Configuration.WEB_API_ROOT + "/api/values/" + i);

                        // Display the response message on the console
                        ConsoleHelper.WriteLineInColor("Response : " + msg, ConsoleColor.Green);
                        eventualSuccesses++;
                    }, cancellationToken);
                }
                catch (Exception e)
                {
                    ConsoleHelper.WriteLineInColor("Request " + i + " eventually failed with: " + e.Message, ConsoleColor.Red);
                    eventualFailures++;
                }

                // Wait half second before the next request.
                await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
            }

            Console.WriteLine("");
            Console.WriteLine("Total requests made                 : " + i);
            Console.WriteLine("Requests which eventually succeeded : " + eventualSuccesses);
            Console.WriteLine("Retries made to help achieve success: " + retries);
            Console.WriteLine("Requests which eventually failed    : " + eventualFailures);

        }
    }
}
