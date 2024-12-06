using SmartPacifier.BackEnd.CommunicationLayer.MQTT;
using SmartPacifier.Interface.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
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
        private bool _isMonitoring = false;
        private bool _isDisposing = false;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly string _outputFilePath;
        private readonly StringBuilder _buffer = new();
        private readonly object _bufferLock = new();

        public event PropertyChangedEventHandler PropertyChanged;

        private string _liveDataOutput;
        public string LiveDataOutput
        {
            get => _liveDataOutput;
            set
            {
                _liveDataOutput = value;
                OnPropertyChanged(nameof(LiveDataOutput));
            }
        }

        private double _scrollingSpeed = 5; // Default scrolling speed
        public double ScrollingSpeed
        {
            get => _scrollingSpeed;
            set
            {
                _scrollingSpeed = value;
                OnPropertyChanged(nameof(ScrollingSpeed));
            }
        }

        public AlgoLiveData(string campaignName, IDatabaseService databaseService)
        {
            InitializeComponent();
            _campaignName = campaignName;
            _databaseService = databaseService;

            Loaded += OnControlLoaded;
            Unloaded += OnControlUnloaded;

            DataContext = this;

            _outputFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "OutputResources", "PythonFiles", "AlgoOutput.txt");
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("AlgoLiveData: Resources initialized on load.");
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            Dispose(); // Dispose resources
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
                // Reset CancellationTokenSource if it was disposed
                _cancellationTokenSource = new CancellationTokenSource();

                SubscribeToBroker(); // Subscribe to broker
                StartUIUpdateTask(_cancellationTokenSource.Token); // Start UI update task
                Debug.WriteLine("Monitoring started.");
                AppendToBuffer("\nMonitoring started successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting monitoring: {ex.Message}");
                AppendToBuffer($"\nError starting monitoring: {ex.Message}");
                _isMonitoring = false;
            }
        }
        private void AppendToBuffer(string data)
        {
            lock (_bufferLock)
            {
                _buffer.AppendLine(data);
            }
        }

        private void StopMonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMonitoring)
            {
                Debug.WriteLine("Monitoring is not running.");
                return;
            }

            _isMonitoring = false;

            SaveRemainingDataToFile();
            UnsubscribeFromBroker();
            _cancellationTokenSource.Cancel();

            AppendToBuffer("Monitoring stopped successfully.");
        }

        private void SubscribeToBroker()
        {
            if (!_isDisposing)
            {
                Broker.Instance.MessageReceived += OnMessageReceived; // Hook up the event
                Debug.WriteLine("Subscribed to broker messages.");
                AppendToBuffer("Subscribed to broker messages.");
            }
        }

        private void UnsubscribeFromBroker()
        {
            if (Broker.Instance != null)
            {
                try
                {
                    Broker.Instance.MessageReceived -= OnMessageReceived;
                    Debug.WriteLine("Unsubscribed from broker messages.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error unsubscribing from broker: {ex.Message}");
                }
            }
        }

        private void OnMessageReceived(object? sender, Broker.MessageReceivedEventArgs e)
        {
            if (_isDisposing || !_isMonitoring) return;

            var liveDataJson = JsonSerializer.Serialize(new
            {
                PacifierId = e.PacifierId,
                SensorType = e.SensorType,
                Data = e.ParsedData
            });

            lock (_bufferLock)
            {
                _buffer.AppendLine($"Received Data: {liveDataJson}");
            }

            // Save to file immediately in the background
            Task.Run(() => SaveDataToFile($"Received Data: {liveDataJson}"));
        }

        private void StartUIUpdateTask(CancellationToken token)
        {
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(500, token); // Update the UI every 500ms

                    string bufferedData;
                    lock (_bufferLock)
                    {
                        bufferedData = _buffer.ToString();
                        _buffer.Clear();
                    }

                    if (!string.IsNullOrEmpty(bufferedData))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            LiveDataOutput += bufferedData;
                            LiveDataScrollViewer?.ScrollToEnd();
                        });
                    }
                }
            }, token);
        }

        private void SaveDataToFile(string data)
        {
            try
            {
                File.AppendAllText(_outputFilePath, data + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving data to file: {ex.Message}");
            }
        }

        private void SaveRemainingDataToFile()
        {
            try
            {
                lock (_bufferLock)
                {
                    if (_buffer.Length > 0)
                    {
                        File.AppendAllText(_outputFilePath, _buffer.ToString());
                        _buffer.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving remaining data to file: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _isDisposing = true;
            StopMonitoringButton_Click(this, new RoutedEventArgs()); // Stop all monitoring processes
            Debug.WriteLine("AlgoLiveData: Disposed all resources.");
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
