﻿namespace PollyTestClient.Samples
{
    /// <summary>
    /// Imagine a microservice or web front end (the upstream caller) trying to call two endpoints on a downstream system.
    /// The 'good' endpoint responds quickly.  The 'faulting' endpoint either faults or responds slowly.
    /// Imagine the _caller_ has limited capacity (all single instances of upstream services/webapps eventually hit some capacity limit).
    ///
    /// This demo 10 does not separate call streams into separate concurrency limiters.
    /// A random combination of calls to the 'good' and 'faulting' endpoint are made.
    ///
    /// Observations:
    /// Because concurrency limiters do not isolate the separate streams of calls,
    /// eventually all the caller's capacity is taken up waiting on the 'faulting' downstream calls.
    /// So the performance of 'good' calls is starved of resource, and starts suffering too.
    /// Watch how the number of pending and failing calls to the good endpoint also climbs,
    /// as the faulting calls saturate all resources in the caller.
    /// </summary>
    public static class BulkheadAsyncDemo00_NoBulkhead
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
            Console.WriteLine(typeof(BulkheadAsyncDemo00_NoBulkhead).Name);
            Console.WriteLine("=======");

            // Let's imagine this caller has some theoretically limited capacity.
            const int callerParallelCapacity = 8; // artificially low - but easier to follow to illustrate the principle
            var limitedCapacityCaller = new LimitedConcurrencyLevelTaskScheduler(callerParallelCapacity);

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

                if (rand.Next(0, 2) == 0)
                //if (i % 2 == 0)
                {
                    goodRequestsMade++;

                    tasks.Add(Task.Factory.StartNew(async j =>
                    {

                        try
                        {
                            string msg = (await client.GetAsync(Configuration.WEB_API_ROOT + "/api/nonthrottledgood/" + j, combinedToken)).Content.ReadAsStringAsync().Result;
                            if (!combinedToken.IsCancellationRequested) ConsoleHelper.WriteLineInColor("Response : " + msg, ConsoleColor.Green);

                            goodRequestsSucceeded++;
                        }
                        catch (Exception e)
                        {
                            if (!combinedToken.IsCancellationRequested) ConsoleHelper.WriteLineInColor("Request " + j + " eventually failed with: " + e.Message, ConsoleColor.Red);

                            goodRequestsFailed++;
                        }
                    }, i, combinedToken, TaskCreationOptions.LongRunning, limitedCapacityCaller).Unwrap()
                    );

                }
                else
                {
                    faultingRequestsMade++;

                    tasks.Add(Task.Factory.StartNew(async j =>
                    {
                        try
                        {
                            string msg = (await client.GetAsync(Configuration.WEB_API_ROOT + "/api/nonthrottledfaulting/" + j, combinedToken)).Content.ReadAsStringAsync().Result;
                            if (!combinedToken.IsCancellationRequested) ConsoleHelper.WriteLineInColor("Response : " + msg, ConsoleColor.Green);

                            faultingRequestsSucceeded++;
                        }
                        catch (Exception e)
                        {
                            if (!combinedToken.IsCancellationRequested) ConsoleHelper.WriteLineInColor("Request " + j + " eventually failed with: " + e.Message, ConsoleColor.Red);

                            faultingRequestsFailed++;
                        }
                    }, i, combinedToken, TaskCreationOptions.LongRunning, limitedCapacityCaller).Unwrap()
                    );

                }

                OutputState();

                await Task.Delay(TimeSpan.FromSeconds(0.1), externalCancellationToken);
            }

            Console.WriteLine("");
            Console.WriteLine("Total requests made                 : " + i);
            Console.WriteLine("");
            OutputState();
            Console.WriteLine("");

            // Cancel any unstarted and running tasks.
            internalCancellationTokenSource.Cancel();
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch
            {
                // Swallow any shutdown exceptions eg TaskCanceledException - we don't care - we are shutting down the demo.
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
