using SmartPacifier.BackEnd.CommunicationLayer.MQTT;
using SmartPacifier.Interface.Services;
using System;
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
        private bool _isMonitoring = false;
        private bool _isDisposing = false;
        private CancellationTokenSource _scrollCancellationTokenSource;
        private readonly string _outputFilePath;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _liveDataOutput;
        public string LiveDataOutput
        {
            get => _liveDataOutput;
            set
            {
                _liveDataOutput = value;
                OnPropertyChanged(nameof(LiveDataOutput));

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!_isDisposing && LiveDataOutputTextBox != null && LiveDataOutputTextBox.IsLoaded)
                    {
                        LiveDataOutputTextBox.ScrollToEnd();
                    }
                });
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
                RestartAutoScrolling(); // Restart auto-scrolling with updated speed
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

            _outputFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "OutputResources", "PythonFiles", "ExecutableScript", "AlgoOutput.txt");

            StartAutoScrolling(); // Initialize auto-scrolling
            ScrollingSpeedSlider.ValueChanged += ScrollingSpeedSlider_ValueChanged;

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
                if (_scrollCancellationTokenSource == null || _scrollCancellationTokenSource.IsCancellationRequested)
                {
                    _scrollCancellationTokenSource = new CancellationTokenSource();
                }

                SubscribeToBroker(); // Subscribe to broker
                StartAutoScrolling(); // Start scrolling
                Debug.WriteLine("Monitoring started.");
                LiveDataOutput += "\nMonitoring started successfully.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting monitoring: {ex.Message}");
                LiveDataOutput += $"\nError starting monitoring: {ex.Message}";
                _isMonitoring = false;
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

            StopAutoScrolling();
            SaveRemainingDataToFile();
            UnsubscribeFromBroker();

            LiveDataOutput += "Monitoring stopped successfully.";
        }

        private void SubscribeToBroker()
        {
            if (!_isDisposing)
            {
                Broker.Instance.MessageReceived += OnMessageReceived; // Hook up the event
                Debug.WriteLine("Subscribed to broker messages.");
                LiveDataOutput += "Subscribed to broker messages.";
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

            // Append received data to the UI output and save it to the file
            Application.Current.Dispatcher.Invoke(() =>
            {
                LiveDataOutput += $"\nReceived Data: {liveDataJson}";
                SaveDataToFile($"Received Data: {liveDataJson}"); // Save to file immediately
            });
        }

        private void StopAutoScrolling()
        {
            try
            {
                if (_scrollCancellationTokenSource != null && !_scrollCancellationTokenSource.IsCancellationRequested)
                {
                    _scrollCancellationTokenSource.Cancel();
                    _scrollCancellationTokenSource.Dispose();
                }
                _scrollCancellationTokenSource = null; // Reset for reuse
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping auto-scrolling: {ex.Message}");
            }
        }

        private void AdjustTextBoxHeight()
        {
            // Increase the height of the TextBox dynamically
            if (LiveDataOutputTextBox != null && LiveDataOutputTextBox.IsLoaded)
            {
                // Adjust height based on the scrolling speed slider
                double newHeight = LiveDataOutputTextBox.ActualHeight + (10 / ScrollingSpeed);
                LiveDataOutputTextBox.Height = newHeight > 600 ? 600 : newHeight; // Cap height at 600
            }
        }

        private void StartAutoScrolling()
        {
            if (_scrollCancellationTokenSource == null)
            {
                _scrollCancellationTokenSource = new CancellationTokenSource();
            }

            var token = _scrollCancellationTokenSource.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay((int)(1000 / _scrollingSpeed), token);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LiveDataScrollViewer?.ScrollToEnd();
                    });
                }
            }, token);
        }

        private void RestartAutoScrolling()
        {
            StopAutoScrolling();
            StartAutoScrolling();
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
                if (!string.IsNullOrEmpty(LiveDataOutput))
                {
                    File.AppendAllText(_outputFilePath, LiveDataOutput);
                    LiveDataOutput = string.Empty; // Clear UI buffer
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

        private void ScrollingSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScrollingSpeed = e.NewValue;

            string speedText = ScrollingSpeed switch
            {
                <= 3 => "Slow",
                <= 7 => "Normal",
                _ => "Fast"
            };

            if (SpeedDisplay != null)
            {
                SpeedDisplay.Text = $"Speed: {speedText}";
            }
        }


    }
}
