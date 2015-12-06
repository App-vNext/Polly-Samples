using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates using the RetryForever policy.
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// </summary>
    public static class RetryForever
    {
        public static void Execute()
        {
            // Let's call a web api service to send 20 messages to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.
            var client = new WebClient();
            int successfulAttempts = 0;
            int failedAttempts = 0;

            // Go into a loop 20 times.
            for (int i = 0; i < 20; i++)
            {
                // Retry the following call repeatedly until it succeeds.
                Policy.Handle<Exception>().RetryForever(Exception =>
                {
                    // This is your new exception handler! 
                    // Tell the user what they've won!
                    Console.WriteLine(Exception.Message);
                    failedAttempts++;

                    // Wait 1 second;
                    System.Threading.Thread.Sleep(1000);

                }).Execute(() =>
                {
                    // This code is executed within the Policy 

                    // Make a request and get a response
                    var msg = client.DownloadString(Configuration.WEB_API_ROOT + "/api/values/" + i.ToString());

                    // Display the response message on the console
                    Console.WriteLine(msg);
                    successfulAttempts++;

                    // Wait one second
                    System.Threading.Thread.Sleep(1000);
                });
            }

            Console.WriteLine("");
            Console.WriteLine("Successful attempts: " + successfulAttempts.ToString());
            Console.WriteLine("Failed attempts: " + failedAttempts.ToString());

        }
    }
}
