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
            // Let's call a web api service to send 20 messages to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            var client = new WebClient();

            // Go into a loop 20 times.
            for (int i = 0; i < 20; i++)
            {
                // Retry the following call repeatedly until it succeeds.
                Policy.Handle<Exception>().RetryForever(Exception =>
                {
                    // This is your new exception handler! 
                    // Tell the user what they've won!
                    Console.WriteLine(Exception.Message);

                    // Wait 1 second;
                    System.Threading.Thread.Sleep(1000);

                }).Execute(() =>
                {
                    // This code is executed within the Policy 

                    // Make a request and get a response
                    var msg = client.DownloadString("http://pollytestapp.azurewebsites.net/api/values/" + i.ToString());
                    
                    // Display the response message on the console
                    Console.WriteLine(msg);

                    // Wait one second
                    System.Threading.Thread.Sleep(1000);

                });
            }
        }
    }
}
