using SmartPacifier.BackEnd.CommunicationLayer.MQTT;
using SmartPacifier.Interface.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Tabs.AlgorithmTab.AlgoExtra
{
    public partial class AlgoLiveData : UserControl, INotifyPropertyChanged
    {
        private readonly string _campaignName;
        private readonly IDatabaseService _databaseService;
        private readonly PythonScriptEngine _pythonScriptEngine;

        // Background processing for throttling
        private readonly BlockingCollection<string> _dataQueue = new BlockingCollection<string>();
        private CancellationTokenSource _cancellationTokenSource;
        private Task _throttleTask;

        public event PropertyChangedEventHandler PropertyChanged;

        // Properties for script selection and output
        public ObservableCollection<string> PythonScripts { get; set; } = new ObservableCollection<string>();

        private string _selectedScript;
        public string SelectedScript
        {
            get => _selectedScript;
            set
            {
                _selectedScript = value;
                OnPropertyChanged(nameof(SelectedScript));
            }
        }

        private string _liveDataOutput;
        public string LiveDataOutput
        {
            get => _liveDataOutput;
            set
            {
                _liveDataOutput = value;
                OnPropertyChanged(nameof(LiveDataOutput));

                // Ensure auto-scroll works properly
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (LiveDataOutputTextBox != null && LiveDataOutputTextBox.IsLoaded)
                    {
                        LiveDataOutputTextBox.ScrollToEnd();
                    }
                    else
                    {
                        Debug.WriteLine("LiveDataOutputTextBox is not initialized or loaded.");
                    }
                });
            }
        }

        private int _throttleSpeedSeconds = 0; // Default to fastest speed
        public int ThrottleSpeedSeconds
        {
            get => _throttleSpeedSeconds;
            set
            {
                _throttleSpeedSeconds = value;
                OnPropertyChanged(nameof(ThrottleSpeedSeconds));
            }
        }

        public AlgoLiveData(string campaignName, IDatabaseService databaseService)
        {
            InitializeComponent();
            _campaignName = campaignName;
            _databaseService = databaseService;
            _pythonScriptEngine = new PythonScriptEngine();

            Loaded += OnControlLoaded;
            DataContext = this;
            LoadAvailableScripts();
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            LoadAvailableScripts();
        }

        // Load available Python scripts into the ComboBox
        private void LoadAvailableScripts()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string scriptsDirectory = Path.Combine(baseDirectory, @"..\..\..\Resources\OutputResources\PythonFiles\ExecutableScript");
            scriptsDirectory = Path.GetFullPath(scriptsDirectory);

            if (Directory.Exists(scriptsDirectory))
            {
                var scriptFiles = Directory.GetFiles(scriptsDirectory, "*.py");
                foreach (var script in scriptFiles)
                {
                    if (!PythonScripts.Contains(Path.GetFileName(script)))
                    {
                        PythonScripts.Add(Path.GetFileName(script));
                    }
                }
            }
            else
            {
                MessageBox.Show($"Scripts directory not found at: {scriptsDirectory}", "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (PythonScripts.Count > 0)
            {
                SelectedScript = PythonScripts[0];
            }
        }

        // Start monitoring live data
        private void StartMonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedScript))
            {
                MessageBox.Show("Please select a script to run.", "No Script Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SubscribeToBroker();
            StartThrottledDataProcessing();
            LiveDataOutput = $"Monitoring live data using script: {SelectedScript}";
        }

        // Stop monitoring live data
        private void StopMonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            StopThrottledDataProcessing();
            Broker.Instance.MessageReceived -= OnMessageReceived;
            LiveDataOutput += "\nMonitoring stopped.";
        }

        // Subscribe to the broker to receive real-time data
        private void SubscribeToBroker()
        {
            Broker.Instance.MessageReceived += OnMessageReceived;
            Debug.WriteLine("AlgoLiveData: Subscribed to broker messages.");
        }

        // Handle incoming broker messages and send data to Python for processing
        private async void OnMessageReceived(object? sender, Broker.MessageReceivedEventArgs e)
        {
            var liveDataJson = JsonSerializer.Serialize(new
            {
                PacifierId = e.PacifierId,
                SensorType = e.SensorType,
                Data = e.ParsedData
            });

            await SendDataToPythonScript(liveDataJson);
        }

        private async Task SendDataToPythonScript(string liveDataJson)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "live_data_script_log.txt");

            try
            {
                File.AppendAllText(logPath, "Sending live data to Python script\n");

                var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\OutputResources\PythonFiles\ExecutableScript", SelectedScript);
                string result = await _pythonScriptEngine.ExecuteScriptWithTcpAsync(scriptPath, liveDataJson);
                File.AppendAllText(logPath, $"Python script executed, result: {result}\n");

                _dataQueue.Add(result);
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error executing Python script with live data: {ex.Message}";
                File.AppendAllText(logPath, errorMsg + "\nDetails:\n" + ex.StackTrace);
                MessageBox.Show(errorMsg, "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartThrottledDataProcessing()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _throttleTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (_dataQueue.TryTake(out var data, Timeout.Infinite, _cancellationTokenSource.Token))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LiveDataOutput += $"\n\nProcessed Output:\n{data}";

                                if (LiveDataOutputTextBox != null && LiveDataOutputTextBox.IsLoaded)
                                {
                                    LiveDataOutputTextBox.ScrollToEnd();
                                }
                            });

                            await Task.Delay(ThrottleSpeedSeconds * 1000, _cancellationTokenSource.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during throttled data processing: {ex.Message}");
                    }
                }
            });
        }

        private void StopThrottledDataProcessing()
        {
            _cancellationTokenSource?.Cancel();
            _throttleTask?.Wait();
            _throttleTask?.Dispose();
            _cancellationTokenSource?.Dispose();
        }

        // Handle throttle slider value changes
        private void ThrottleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ThrottleSpeedSeconds = 10 - (int)e.NewValue;
            Debug.WriteLine($"Throttle speed updated to: {ThrottleSpeedSeconds} seconds");
        }

        // Raise the PropertyChanged event to notify the UI about property updates
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
