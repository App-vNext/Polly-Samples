using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using Polly.Bulkhead;

namespace PollyTestClient.Samples
{
    /// <summary>
    /// Imagine a microservice or web front end (the upstream caller) trying to call two endpoints on a downstream system.
    /// The 'good' endpoint responds quickly.  The 'faulting' endpoint faults, and responds slowly.
    /// Imagine the _caller_ has limited capacity (all services/webapps eventually hit some capacity limit).
    /// 
    /// Compared to demo 00, this demo 01 isolates the calls to the 'good' and 'faulting'
    /// endpoints into separate bulkheads.
    /// A random combination of calls to the 'good' and 'faulting' endpoint are made.
    /// Because the separate 'good' and 'faulting' streams are isolated
    /// in separate bulkheads, the 'faulting' calls back up,
    /// but the throughput for 'good' calls in unaffected.
    /// 
    /// Bulkhead: one fault doesn't sink the whole ship!
    /// </summary>
    public static class BulkheadAsyncDemo01_WithBulkheads
    {

        // Track the number of 'good' and 'faulting' requests made, succeeded and failed.
        // At any time, requests pending = made - succeeded - failed.
        static int goodRequestsMade = 0;
        static int goodRequestsSucceeded = 0;
        static int goodRequestsFailed = 0;
        static int faultingRequestsMade = 0;
        static int faultingRequestsSucceeded = 0;
        static int faultingRequestsFailed = 0;

        public static async Task ExecuteAsync(CancellationToken externalCancellationToken)
        {
            Console.WriteLine(typeof(BulkheadAsyncDemo01_WithBulkheads).Name);
            Console.WriteLine("=======");

            // Let's imagine this caller has some theoretically limited capacity.
            const int callerParallelCapacity = 8; // (artificially low - but easier to follow, to illustrate principle)
            var limitedCapacityCaller = new LimitedConcurrencyLevelTaskScheduler(callerParallelCapacity);

            BulkheadPolicy bulkheadForGoodCalls = Policy.BulkheadAsync(callerParallelCapacity/2, int.MaxValue);
            BulkheadPolicy bulkheadForFaultingCalls = Policy.BulkheadAsync(callerParallelCapacity - callerParallelCapacity/2, int.MaxValue);

            var client = new HttpClient();
            var rand = new Random();
            int i = 0;

            IList<Task> tasks = new List<Task>();
            CancellationTokenSource internalCancellationTokenSource = new CancellationTokenSource();
            CancellationToken combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
                externalCancellationToken, internalCancellationTokenSource.Token).Token;

            while (!Console.KeyAvailable && !externalCancellationToken.IsCancellationRequested)
            {
                i++;

                // Randomly make either 'good' or 'faulting' calls.
                if (rand.Next(0, 2) == 0)
                //if (i % 2 == 0)
                {
                    goodRequestsMade++;
                    // Call 'good' endpoint.
                    tasks.Add(Task.Factory.StartNew(j =>

                        bulkheadForGoodCalls.ExecuteAsync(async () =>
                        {

                            try
                            {
                                // Make a request and get a response, from the good endpoint
                                string msg = (await client.GetAsync(Configuration.WEB_API_ROOT + "/api/nonthrottledgood/" + j, combinedToken)).Content.ReadAsStringAsync().Result;
                                if (!combinedToken.IsCancellationRequested)
                                {
                                    ConsoleHelper.WriteLineInColor("Response : " + msg, ConsoleColor.Green);
                                }
                                goodRequestsSucceeded++;
                            }
                            catch (Exception e)
                            {
                                if (!combinedToken.IsCancellationRequested)
                                {
                                    ConsoleHelper.WriteLineInColor("Request " + j + " eventually failed with: " + e.Message, ConsoleColor.Red);
                                }
                                goodRequestsFailed++;
                            }
                        }), i, combinedToken, TaskCreationOptions.LongRunning, limitedCapacityCaller).Unwrap()
                    );

                }
                else
                {
                    faultingRequestsMade++;
                    // call 'faulting' endpoint.
                    tasks.Add(Task.Factory.StartNew(j =>

                        bulkheadForFaultingCalls.ExecuteAsync(async () =>
                        {
                            try
                            {
                                // Make a request and get a response, from the faulting endpoint
                                string msg = (await client.GetAsync(Configuration.WEB_API_ROOT + "/api/nonthrottledfaulting/" + j, combinedToken)).Content.ReadAsStringAsync().Result;
                                if (!combinedToken.IsCancellationRequested)
                                {
                                    ConsoleHelper.WriteLineInColor("Response : " + msg, ConsoleColor.Green);
                                }
                                faultingRequestsSucceeded++;
                            }
                            catch (Exception e)
                            {
                                if (!combinedToken.IsCancellationRequested)
                                {
                                    ConsoleHelper.WriteLineInColor("Request " + j + " eventually failed with: " + e.Message, ConsoleColor.Red);
                                }
                                faultingRequestsFailed++;
                            }
                        }), i, combinedToken, TaskCreationOptions.LongRunning, limitedCapacityCaller).Unwrap()
                    );

                }

                OutputState();

                // Wait briefly
                await Task.Delay(TimeSpan.FromSeconds(0.1), externalCancellationToken);
            }   

            Console.WriteLine("");
            Console.WriteLine("Total requests made                 : " + i);
            Console.WriteLine("");
            OutputState();
            Console.WriteLine("");

            // Cancel any unstarted tasks.
            internalCancellationTokenSource.Cancel();
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch 
            {
                // Swallow any shutdown exceptions eg TaskCanceledException - we don't care - we are just shutting down.
            }
        }

        public static void OutputState()
        {
            ConsoleHelper.WriteInColor(String.Format("    Good endpoint: requested {0:00}, ", goodRequestsMade), ConsoleColor.White);
            ConsoleHelper.WriteInColor(String.Format("succeeded {0:00}, ", goodRequestsSucceeded), ConsoleColor.Green);
            ConsoleHelper.WriteInColor(String.Format("pending {0:00}, ", goodRequestsMade - goodRequestsSucceeded - goodRequestsFailed), ConsoleColor.Yellow);
            ConsoleHelper.WriteLineInColor(String.Format("failed {0:00}.", goodRequestsFailed), ConsoleColor.Red);
            
            ConsoleHelper.WriteInColor(String.Format("Faulting endpoint: requested {0:00}, ", faultingRequestsMade), ConsoleColor.White);
            ConsoleHelper.WriteInColor(String.Format("succeeded {0:00}, ", faultingRequestsSucceeded), ConsoleColor.Green);
            ConsoleHelper.WriteInColor(String.Format("pending {0:00}, ", faultingRequestsMade - faultingRequestsSucceeded - faultingRequestsFailed), ConsoleColor.Yellow);
            ConsoleHelper.WriteLineInColor(String.Format("failed {0:00}.", faultingRequestsFailed), ConsoleColor.Red);

            Console.WriteLine();
        }
    }
}
