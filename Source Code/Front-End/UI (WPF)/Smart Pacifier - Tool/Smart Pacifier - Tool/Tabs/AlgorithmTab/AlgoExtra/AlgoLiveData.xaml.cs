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

        // Properties for live data source selection and output
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

                // Automatically scroll the TextBox to the bottom
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LiveDataOutputTextBox?.ScrollToEnd();
                });
            }
        }

        private int _throttleSpeedSeconds = 1; // Default throttle speed in seconds
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

            DataContext = this;
            LoadAvailableScripts();
            SubscribeToBroker();
            StartThrottledDataProcessing();
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
                    PythonScripts.Add(Path.GetFileName(script));
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

        // Subscribe to the broker to receive real-time data
        private void SubscribeToBroker()
        {
            Broker.Instance.MessageReceived += OnMessageReceived;
            Debug.WriteLine("AlgoLiveData: Subscribed to broker messages.");
        }

        // Handle incoming broker messages and send data to Python for processing
        private async void OnMessageReceived(object? sender, Broker.MessageReceivedEventArgs e)
        {
            // Convert parsed data to JSON format
            var liveDataJson = JsonSerializer.Serialize(new
            {
                PacifierId = e.PacifierId,
                SensorType = e.SensorType,
                Data = e.ParsedData
            });

            // Send data to the Python script for additional processing
            await SendDataToPythonScript(liveDataJson);
        }

        private async Task SendDataToPythonScript(string liveDataJson)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "live_data_script_log.txt");

            try
            {
                File.AppendAllText(logPath, "Sending live data to Python script\n");

                // Define the Python script path
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var scriptPath = Path.Combine(baseDirectory, @"Resources\OutputResources\PythonFiles\ExecutableScript", SelectedScript);

                // Execute the Python script, passing the live data JSON
                string result = await _pythonScriptEngine.ExecuteScriptWithTcpAsync(scriptPath, liveDataJson);
                File.AppendAllText(logPath, $"Python script executed, result: {result}\n");

                // Add the processed data to the queue
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
                            // Update the UI with throttled data
                            LiveDataOutput += $"\n\nProcessed Output:\n{data}";

                            // Respect throttle speed (convert seconds to milliseconds)
                            await Task.Delay(ThrottleSpeedSeconds * 1000, _cancellationTokenSource.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break; // Exit the loop if cancellation is requested
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

        // Start monitoring live data
        private void StartMonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedScript))
            {
                MessageBox.Show("Please select a script to run.", "No Script Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LiveDataOutput = $"Monitoring live data using script: {SelectedScript}";
        }

        // Handle throttle slider value changes
        private void ThrottleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Convert slider value into seconds (invert logic: higher slider = faster update)
            ThrottleSpeedSeconds = 10 - (int)e.NewValue; // Example: Slider max = 10, min = 0
            Debug.WriteLine($"Throttle speed updated to: {ThrottleSpeedSeconds} seconds");
        }

        // Handle cleanup when the control is unloaded
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopThrottledDataProcessing();
        }

        // Raise the PropertyChanged event to notify the UI about property updates
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
