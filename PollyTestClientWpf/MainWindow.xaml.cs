using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using PollyDemos;
using PollyDemos.Async;
using PollyDemos.OutputHelpers;
using PollyDemos.Sync;
using Color = PollyDemos.OutputHelpers.Color;

namespace PollyTestClientWpf;

public partial class MainWindow : Window
{
    private CancellationTokenSource? cancellationSource;

    private readonly Type[] availableDemoTypes;

    private readonly object lockObject = new();

    private Statistic[] closingStatistics = Array.Empty<Statistic>();

    private const int MaxStatisticsToShow = 9;
    private const string StatisticBoxPrefix = "Statistic";
    private const string StatisticLabelPrefix = "StatisticLabel";

    private readonly Progress<DemoProgress> progress;

    public MainWindow()
    {
        InitializeComponent();

        availableDemoTypes = Assembly.GetAssembly(typeof(DemoBase))!
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(DemoBase)) && !t.IsAbstract)
            .ToArray();

        progress = new Progress<DemoProgress>();
        progress.ProgressChanged += (sender, args) =>
        {
            Output.Dispatcher.Invoke(() =>
            {
                WriteMultiLineInColor(args.Messages);
                UpdateStatistics(args.Statistics);
            });

            closingStatistics = args.Statistics;
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Output.Dispatcher.Invoke(() =>
            {
                StopButton_Click(this, new());
                WriteLineInColor($"Unobserved task exception: {args.Exception.Flatten()}.", Color.Red);
            });

            args.SetObserved();
        };

        Demo.SelectedIndex = 0;
    }

    private Type? GetDemoType(string demoName)
        => availableDemoTypes.SingleOrDefault(type => type.Name == demoName);

    private void ClearButton_Click(object sender, RoutedEventArgs args)
    {
        Output.Document = new FlowDocument();
        for (var i = 0; i < MaxStatisticsToShow; i++)
        {
            var statSuffix = $"{i:00}";
            var label = (Label)FindName(StatisticLabelPrefix + statSuffix);
            label.Content = string.Empty;

            var statBox = (TextBox)FindName(StatisticBoxPrefix + statSuffix);
            statBox.Text = string.Empty;
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs args)
    {
        ClearButton_Click(this, new());
        StopButton.IsEnabled = true;
        PlayButton.IsEnabled = false;

        cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;

        if (Demo.SelectedItem is not ComboBoxItem selectedItem)
        {
            WriteLineInColor("No demo selected.", Color.Red);
            cancellationSource.Cancel();
            return;
        }

        var demoName = selectedItem.Name;
        var demoType = GetDemoType(demoName);
        if (demoType == null)
        {
            WriteLineInColor($"Unable to identify demo: {demoName}", Color.Red);
            cancellationSource.Cancel();
            return;
        }

        if (demoType.IsSubclassOf(typeof(SyncDemo)))
        {
            SyncDemo? demoInstance = null;
            try
            {
                demoInstance = Activator.CreateInstance(demoType) as SyncDemo;
            }
            catch (Exception) {}

            if (demoInstance is null)
            {
                WriteLineInColor($"Unable to instantiate demo: {demoName}", Color.Red);
                cancellationSource.Cancel();
                return;
            }

            try
            {
                Task.Factory
                    .StartNew(
                        () => demoInstance.Execute(cancellationToken, progress),
                        cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                    .ContinueWith(t => HandleFailedTask(t, demoName),
                        CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);
            }
            catch (Exception e)
            {
                WriteLineInColor($"Demo {demoName} threw exception: {e}", Color.Red);
            }

            return;
        }

        if (demoType.IsSubclassOf(typeof(AsyncDemo)))
        {
            AsyncDemo? demoInstance = null;
            try
            {
                demoInstance = Activator.CreateInstance(demoType) as AsyncDemo;
            }
            catch (Exception) {}

            if (demoInstance is null)
            {
                WriteLineInColor($"Unable to instantiate demo: {demoName}", Color.Red);
                cancellationSource.Cancel();
                return;
            }

            Task.Factory
                .StartNew(
                    () => demoInstance.ExecuteAsync(cancellationToken, progress),
                    cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .Unwrap()
                .ContinueWith(t => HandleFailedTask(t, demoName),
                    CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);

            return;
        }

        WriteLineInColor($"Unable to identify demo as either sync or async demo: {demoName}", Color.Red);
        cancellationSource.Cancel();
    }

    private void HandleFailedTask(Task task, string demoName)
    {
        var message = string.Empty;
        if (task.IsCanceled) message = $"Demo was canceled: {demoName}";
        if (task.IsFaulted) message = $"Demo {demoName} threw exception: {task.Exception}";
        WriteLineInColor(message, Color.Red);
    }

    private void StopButton_Click(object sender, RoutedEventArgs args)
    {
        StopButton.IsEnabled = false;
        PlayButton.IsEnabled = true;

        if (cancellationSource == null)
        {
            WriteLineInColor($"No demo currently running.", Color.Red);
            return;
        }

        try
        {
            cancellationSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            WriteLineInColor($"Demo already stopped.", Color.Red);
            return;
        }

        if (closingStatistics.Any())
        {
            int longestDescription = closingStatistics.Max(s => s.Description.Length);
            foreach (Statistic stat in closingStatistics)
            {
                WriteLineInColor(stat.Description.PadRight(longestDescription) + ": " + stat.Value, stat.Color);
            }

        }

        cancellationSource.Dispose();
    }

    private void Demo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Description.Text = string.Empty;
        var comboBox = (ComboBox)sender;
        var demoName = ((ComboBoxItem)comboBox.SelectedItem).Name;
        var demoType = GetDemoType(demoName);
        if (demoType == null)
        {
            return;
        }

        DemoBase? demoInstance = null;
        try
        {
            demoInstance = Activator.CreateInstance(demoType) as DemoBase;
        }
        catch (Exception) { }

        if (demoInstance is null) return;

        Description.Text = demoInstance.Description;
    }

    public void WriteMultiLineInColor(IEnumerable<ColoredMessage> messages)
    {
        Output.Dispatcher.Invoke(() =>
        {
            lock (lockObject) // Locking helps avoid the color of one message leaking onto another, in multi-threaded callbacks.
            {
                foreach (var message in messages)
                {
                    var newText = new TextRange(Output.Document.ContentEnd, Output.Document.ContentEnd)
                    {
                        Text = $"{message.Message}\n\r"
                    };
                    newText.ApplyPropertyValue(TextElement.ForegroundProperty, message.Color.ToBrushColor());
                }
            }

            Output.ScrollToEnd();
        });
    }

    public void WriteLineInColor(string message, Color color)
    {
        Output.Dispatcher.Invoke(() =>
        {
            lock (lockObject) // Locking helps avoid the color of one message leaking onto another, in multi-threaded callbacks.
            {
                var newText = new TextRange(Output.Document.ContentEnd, Output.Document.ContentEnd)
                {
                    Text = $"{message}\n\r"
                };
                newText.ApplyPropertyValue(TextElement.ForegroundProperty, color.ToBrushColor());
            }

            Output.ScrollToEnd();
        });
    }

    private void UpdateStatistics(Statistic[] statistics)
    {
        Output.Dispatcher.Invoke(() =>
        {
            var statisticsToShow = statistics.Length;
            for (var i = 0; i < MaxStatisticsToShow; i++)
            {
                var statSuffix = $"{i:00}";
                var label = (Label)FindName(StatisticLabelPrefix + statSuffix);
                var statBox = (TextBox)FindName(StatisticBoxPrefix + statSuffix);

                if (i >= statisticsToShow)
                {
                    label.Visibility = Visibility.Hidden;
                    statBox.Visibility = Visibility.Hidden;
                    return;
                }

                var statistic = statistics[i];
                label.Content = statistic.Description;
                statBox.Foreground = statistic.Color.ToBrushColor();
                statBox.Text = statistic.Value.ToString().PadLeft(3);
                label.Visibility = Visibility.Visible;
                statBox.Visibility = Visibility.Visible;
            }
        });
    }
}
