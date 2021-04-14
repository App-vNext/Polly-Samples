using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PollyDemos;
using PollyDemos.Async;
using PollyDemos.OutputHelpers;
using PollyDemos.Sync;
using Color = PollyDemos.OutputHelpers.Color;

namespace PollyTestClientWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationSource;
        private CancellationToken cancellationToken;

        private static readonly Type[] availableDemoTypes = Assembly.GetAssembly(typeof(DemoBase)).GetTypes()
            .Where(t => t.IsSubclassOf(typeof(DemoBase)) && !t.IsAbstract).ToArray();

        private readonly object lockObject = new object();

        private Statistic[] closingStatistics = new Statistic[0];

        private const int MaxStatisticsToShow = 9;
        private const string StatisticBoxPrefix = "Statistic";
        private const string StatisticLabelPrefix = "StatisticLabel";

        private Progress<DemoProgress> progress;

        public MainWindow()
        {
            InitializeComponent();

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            PlayButton.Click += (sender, args) => PlayButton_Click();
            StopButton.Click += (sender, args) => StopButton_Click();
            ClearButton.Click += (sender, args) => ClearButton_Click();

            progress = new Progress<DemoProgress>();
            progress.ProgressChanged += (sender, progressArgs) =>
            {
                Output.Dispatcher.Invoke(() =>
                {
                    WriteMultiLineInColor(progressArgs.Messages);

                    UpdateStatistics(progressArgs.Statistics);
                });

                closingStatistics = progressArgs.Statistics;
            };

            Demo.SelectedIndex = 0;

            ClearButton_Click();
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Output.Dispatcher.Invoke(() =>
            {
                StopButton_Click();
                WriteLineInColor($"Unobserved task exception: {e.Exception.Flatten()}.", Color.Red);
            });

            e.SetObserved();
        }

        private static Type GetDemoType(string demoName)
        {
            return availableDemoTypes.SingleOrDefault(t => t.Name == demoName);
        }

        private void ClearButton_Click()
        {
            Output.Document = new FlowDocument();
            UpdateStatistics(new Statistic[0]);
        }

        private void PlayButton_Click()
        {
            StopButton.IsEnabled = true;
            PlayButton.IsEnabled = false;

            cancellationSource = new CancellationTokenSource();

            cancellationToken = cancellationSource.Token;

            var selectedItem = Demo.SelectedItem as ComboBoxItem;
            if (selectedItem == null)
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
            }
            else if (demoType.IsSubclassOf(typeof(SyncDemo)))
            {
                SyncDemo demoInstance;
                try
                {
                    demoInstance = Activator.CreateInstance(demoType) as SyncDemo;
                }
                catch (Exception)
                {
                    demoInstance = null;
                }

                if (demoInstance == null)
                {
                    WriteLineInColor($"Unable to instantiate demo: {demoName}", Color.Red);
                    cancellationSource.Cancel();
                    return;
                }

                try
                {
                    Task.Factory.StartNew(() => demoInstance.Execute(cancellationToken, progress), cancellationToken,
                            TaskCreationOptions.LongRunning, TaskScheduler.Default)
                        .ContinueWith(t =>
                        {
                            if (t.IsCanceled)
                                WriteLineInColor($"Demo was canceled: {demoName}", Color.Red);
                            else if (t.IsFaulted)
                                WriteLineInColor($"Demo {demoName} threw exception: {t.Exception.ToString()}",
                                    Color.Red);
                        }, CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);
                }
                catch (Exception e)
                {
                    WriteLineInColor($"Demo {demoName} threw exception: {e}", Color.Red);
                }
            }
            else if (demoType.IsSubclassOf(typeof(AsyncDemo)))
            {
                AsyncDemo demoInstance;
                try
                {
                    demoInstance = Activator.CreateInstance(demoType) as AsyncDemo;
                }
                catch (Exception)
                {
                    demoInstance = null;
                }

                if (demoInstance == null)
                {
                    WriteLineInColor($"Unable to instantiate demo: {demoName}", Color.Red);
                    cancellationSource.Cancel();
                    return;
                }

                Task.Factory.StartNew(() => demoInstance.ExecuteAsync(cancellationToken, progress), cancellationToken,
                        TaskCreationOptions.LongRunning, TaskScheduler.Default)
                    .Unwrap()
                    .ContinueWith(t =>
                    {
                        if (t.IsCanceled)
                            WriteLineInColor($"Demo was canceled: {demoName}", Color.Red);
                        else if (t.IsFaulted)
                            WriteLineInColor($"Demo {demoName} threw exception: {t.Exception.ToString()}", Color.Red);
                    }, CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);
            }
            else
            {
                WriteLineInColor($"Unable to identify demo as either sync or async demo: {demoName}", Color.Red);
                cancellationSource.Cancel();
            }
        }

        private void StopButton_Click()
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

            // Output closing statistics.
            if (closingStatistics.Any())
            {
                int longestDescription = closingStatistics.Max(s => s.Description.Length);
                foreach (Statistic stat in closingStatistics)
                    WriteLineInColor(stat.Description.PadRight(longestDescription) + ": " + stat.Value, stat.Color);
            }

            cancellationSource.Dispose();
        }

        private void Demo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;

            var demoName = (comboBox.SelectedItem as ComboBoxItem).Name;
            var demoType = GetDemoType(demoName);
            if (demoType == null)
            {
                Description.Text = string.Empty;
                return;
            }

            DemoBase demoInstance;
            try
            {
                demoInstance = Activator.CreateInstance(demoType) as DemoBase;
            }
            catch (Exception)
            {
                demoInstance = null;
            }

            if (demoInstance == null)
            {
                Description.Text = string.Empty;
                return;
            }

            Description.Text = demoInstance.Description;
        }

        public void WriteMultiLineInColor(ColoredMessage[] messages)
        {
            Output.Dispatcher.Invoke(() =>
            {
                lock (lockObject) // Locking helps avoid the color of one message leaking onto another, in multi-threaded callbacks.
                    foreach (ColoredMessage message in messages)
                    {
                        var newText = new TextRange(Output.Document.ContentEnd, Output.Document.ContentEnd)
                        {
                            Text = message.Message + "\n"
                        };
                        newText.ApplyPropertyValue(TextElement.ForegroundProperty, message.Color.ToBrushColor());
                    }

                Output.ScrollToEnd();
            });
        }

        public void WriteLineInColor(string msg, Color color)
        {
            Output.Dispatcher.Invoke(() =>
            {
                lock (lockObject) // Locking helps avoid the color of one message leaking onto another, in multi-threaded callbacks.
                {
                    var newText = new TextRange(Output.Document.ContentEnd, Output.Document.ContentEnd)
                    {
                        Text = msg + "\n"
                    };
                    newText.ApplyPropertyValue(TextElement.ForegroundProperty, color.ToBrushColor());
                    Output.ScrollToEnd();
                }
            });
        }

        private void UpdateStatistics(Statistic[] stats)
        {
            Output.Dispatcher.Invoke(() =>
            {
                var statisticsToShow = stats.Length;
                for (var i = 0; i < MaxStatisticsToShow; i++)
                {
                    var statSuffix = $"{i:00}";
                    var label = (Label)FindName(StatisticLabelPrefix + statSuffix);
                    var statBox = (TextBox)FindName(StatisticBoxPrefix + statSuffix);

                    if (i < statisticsToShow)
                    {
                        Statistic statistic = stats[i];
                        label.Content = statistic.Description;
                        statBox.Foreground = statistic.Color.ToBrushColor();
                        statBox.Text = statistic.Value.ToString().PadLeft(3);
                        label.Visibility = Visibility.Visible;
                        statBox.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        label.Visibility = Visibility.Hidden;
                        statBox.Visibility = Visibility.Hidden;
                    }
                }
            });
        }
    }
}