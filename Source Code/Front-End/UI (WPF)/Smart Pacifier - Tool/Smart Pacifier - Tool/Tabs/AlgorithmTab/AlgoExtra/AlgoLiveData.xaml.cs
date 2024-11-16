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
    public partial class AlgoLiveData : UserControl, INotifyPropertyChanged, IDisposable
    {
        private readonly string _campaignName;
        private readonly IDatabaseService _databaseService;
        private PythonScriptEngine _pythonScriptEngine; // Removed readonly

        // Background processing for throttling
        private BlockingCollection<string> _dataQueue = new BlockingCollection<string>(); // Removed 'readonly'
        private CancellationTokenSource _cancellationTokenSource;
        private Task _throttleTask; // Proper declaration of the _throttleTask variable
        private bool _isDisposing = false; // Flag to indicate the application is closing

        public event PropertyChangedEventHandler PropertyChanged;

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
                    if (!_isDisposing && LiveDataOutputTextBox != null && LiveDataOutputTextBox.IsLoaded)
                    {
                        LiveDataOutputTextBox.ScrollToEnd();
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

            // Attach event handlers for lifecycle management
            Loaded += OnControlLoaded;
            Unloaded += OnControlUnloaded;

            DataContext = this;
            LoadAvailableScripts();
        }


        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            // Reinitialize resources
            _dataQueue = new BlockingCollection<string>();
            _cancellationTokenSource = new CancellationTokenSource();
            Debug.WriteLine("AlgoLiveData: Resources reinitialized on load.");
        }


        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            Dispose(); // Ensure all resources are disposed
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
        private void StartMonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isMonitoring)
            {
                Debug.WriteLine("Monitoring is already running.");
                return;
            }

            _isMonitoring = true;

            try
            {
                _dataQueue = new BlockingCollection<string>();
                SubscribeToBroker();
                StartThrottledDataProcessing();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting monitoring: {ex.Message}");
                _isMonitoring = false;
            }
        }


        private bool _isMonitoring = false; // Track monitoring state to avoid repeated stops

        private void StopMonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMonitoring)
            {
                Debug.WriteLine("Monitoring is already stopped. Ignoring stop request.");
                MessageBox.Show("Monitoring is already stopped.", "Stop Monitoring", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Debug.WriteLine("Stopping monitoring...");

            try
            {
                StopThrottledDataProcessing();
                Debug.WriteLine("Throttled data processing stopped.");

                // Do NOT stop the broker if it's not related to the issue
                UnsubscribeFromBroker();
                Debug.WriteLine("Unsubscribed from broker messages.");

                StopPythonScript(); // Ensure Python script stops
                Debug.WriteLine("Python script execution stopped.");

                LiveDataOutput += "\nMonitoring stopped.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during StopMonitoring: {ex.Message}");
                MessageBox.Show($"Error while stopping monitoring:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isMonitoring = false; // Ensure the state is updated
            }
        }
        // Method to stop the Python script execution
        private void StopPythonScript()
        {
            if (_pythonScriptEngine == null) return;

            try
            {
                _pythonScriptEngine.StopExecution();
                Debug.WriteLine("Python script execution stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping Python script: {ex.Message}");
            }
            finally
            {
                _pythonScriptEngine = new PythonScriptEngine();
                Debug.WriteLine("Python script engine reinitialized.");
            }
        }



        // Subscribe to the broker to receive real-time data
        private void SubscribeToBroker()
        {
            if (!_isDisposing)
            {
                Broker.Instance.MessageReceived += OnMessageReceived;
                Debug.WriteLine("AlgoLiveData: Subscribed to broker messages.");
            }
        }

        // Unsubscribe from broker to prevent further data processing
        private void UnsubscribeFromBroker()
        {
            if (Broker.Instance != null)
            {
                try
                {
                    Broker.Instance.MessageReceived -= OnMessageReceived;
                    Debug.WriteLine("AlgoLiveData: Unsubscribed from broker messages.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error unsubscribing from broker: {ex.Message}");
                }
            }
        }

        // Handle incoming broker messages and send data to Python for processing
        private async void OnMessageReceived(object? sender, Broker.MessageReceivedEventArgs e)
        {
            if (_isDisposing) return;

            var liveDataJson = JsonSerializer.Serialize(new
            {
                PacifierId = e.PacifierId,
                SensorType = e.SensorType,
                Data = e.ParsedData
            });

            await SendDataToPythonScript(liveDataJson);

            // Use the UpdateLiveDataOutput method to update the UI safely
            UpdateLiveDataOutput(liveDataJson);
        }

        private void UpdateLiveDataOutput(string data)
        {
            if (_isDisposing || LiveDataOutputTextBox == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    LiveDataOutput += $"\n\nProcessed Output:\n{data}";
                    LiveDataOutputTextBox?.ScrollToEnd();
                }
                catch (ObjectDisposedException)
                {
                    Debug.WriteLine("LiveDataOutputTextBox was accessed after being disposed.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unexpected error updating LiveDataOutput: {ex.Message}");
                }
            });
        }
        private async Task SendDataToPythonScript(string liveDataJson)
        {
            if (_isDisposing) return; // Prevent further processing if disposing

            try
            {
                var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\OutputResources\PythonFiles\ExecutableScript", SelectedScript);
                string result = await _pythonScriptEngine.ExecuteScriptWithTcpAsync(scriptPath, liveDataJson);

                // Add to queue only if it hasn't been completed or disposed
                if (!_isDisposing && !_isDataQueueCompleted && !_dataQueue.IsAddingCompleted)
                {
                    _dataQueue.Add(result);
                }
                else
                {
                    Debug.WriteLine("Data queue is already completed or disposing. Skipping addition.");
                }
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine($"Attempted to access a disposed object: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error executing Python script: {ex.Message}");
                if (!_isDisposing)
                {
                    MessageBox.Show($"Error executing Python script:\n{ex.Message}", "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }




        private void StartThrottledDataProcessing()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                Debug.WriteLine("Throttled data processing is already running. Skipping start.");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _throttleTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (_isDisposing) break;

                        if (_dataQueue.TryTake(out var data, Timeout.Infinite, _cancellationTokenSource.Token))
                        {
                            UpdateLiveDataOutput(data); // Update the UI safely
                            await Task.Delay(ThrottleSpeedSeconds * 1000, _cancellationTokenSource.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("Throttled data processing task canceled.");
                        break;
                    }
                    catch (InvalidOperationException ex)
                    {
                        Debug.WriteLine($"Invalid operation in data processing: {ex.Message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in throttled data processing: {ex.Message}");
                    }
                }
            });
        }


        private bool _isDataQueueCompleted = false; // Track if CompleteAdding has been called
        private void StopThrottledDataProcessing()
        {
            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                Debug.WriteLine("Throttled data processing is already stopped or not started. Skipping...");
                return;
            }

            try
            {
                Debug.WriteLine("Cancelling throttled data processing...");
                _cancellationTokenSource.Cancel();

                if (_throttleTask != null)
                {
                    Task.WaitAny(_throttleTask); // Wait for the task to finish
                    Debug.WriteLine("Throttled data processing task completed.");
                }
            }
            catch (AggregateException ex)
            {
                Debug.WriteLine($"Task cancellation exception: {ex.Message}");
            }
            finally
            {
                _throttleTask?.Dispose();
                _throttleTask = null;

                if (_dataQueue != null && !_dataQueue.IsAddingCompleted)
                {
                    try
                    {
                        _dataQueue.CompleteAdding();
                    }
                    catch (ObjectDisposedException)
                    {
                        Debug.WriteLine("Attempted to complete adding on a disposed data queue.");
                    }
                }

                _dataQueue?.Dispose();
                _dataQueue = new BlockingCollection<string>(); // Reinitialize the data queue
                Debug.WriteLine("Data queue reinitialized.");
            }
        }


        public void Dispose()
        {
            _isDisposing = true; // Prevent further operations
            StopThrottledDataProcessing(); // Stop background tasks
            UnsubscribeFromBroker(); // Unsubscribe from broker messages

            // Dispose of resources
            _dataQueue?.Dispose();
            _dataQueue = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _pythonScriptEngine?.StopExecution();
            _pythonScriptEngine = null;

            Debug.WriteLine("AlgoLiveData: Disposed all resources.");
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
