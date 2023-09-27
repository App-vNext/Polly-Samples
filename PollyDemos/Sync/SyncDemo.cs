using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync
{
    public abstract class SyncDemo : DemoBase
    {
        public abstract void Execute(CancellationToken cancellationToken, IProgress<DemoProgress> progress);
    }
}