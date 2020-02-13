using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Async
{
    public abstract class AsyncDemo : DemoBase
    {
        public abstract Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress);
    }
}