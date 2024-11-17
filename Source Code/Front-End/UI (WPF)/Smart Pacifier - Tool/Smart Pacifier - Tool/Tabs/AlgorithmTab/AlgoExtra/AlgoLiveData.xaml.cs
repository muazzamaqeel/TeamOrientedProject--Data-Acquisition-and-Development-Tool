using SmartPacifier.BackEnd.CommunicationLayer.MQTT;
using SmartPacifier.Interface.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Tabs.AlgorithmTab.AlgoExtra
{
    public partial class AlgoLiveData : UserControl, INotifyPropertyChanged, IDisposable
    {
        private readonly string _campaignName;
        private readonly IDatabaseService _databaseService;

        private bool _isMonitoring = false; // Monitoring state
        private bool _isDisposing = false; // Disposal state

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

        public AlgoLiveData(string campaignName, IDatabaseService databaseService)
        {
            InitializeComponent();
            _campaignName = campaignName;
            _databaseService = databaseService;

            Loaded += OnControlLoaded;
            Unloaded += OnControlUnloaded;

            DataContext = this;
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
                SubscribeToBroker(); // Subscribe to broker
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

        private void SubscribeToBroker()
        {
            if (!_isDisposing)
            {
                Broker.Instance.MessageReceived += OnMessageReceived; // Hook up the event
                Debug.WriteLine("Subscribed to broker messages.");
                LiveDataOutput += "\nSubscribed to broker messages.";
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
            if (_isDisposing) return;

            var liveDataJson = JsonSerializer.Serialize(new
            {
                PacifierId = e.PacifierId,
                SensorType = e.SensorType,
                Data = e.ParsedData
            });

            // Append received data to the UI output
            Application.Current.Dispatcher.Invoke(() =>
            {
                LiveDataOutput += $"\nReceived Data: {liveDataJson}";

                // Auto-scroll to the end of the ScrollViewer
                LiveDataScrollViewer?.ScrollToEnd();
            });
        }


        public void Dispose()
        {
            _isDisposing = true;
            _isMonitoring = false;
            UnsubscribeFromBroker();
            Debug.WriteLine("AlgoLiveData: Disposed all resources.");
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private double _scrollingSpeed = 5; // Default scrolling speed

        private void ScrollingSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _scrollingSpeed = e.NewValue;

            string speedText = _scrollingSpeed switch
            {
                <= 3 => "Slow",
                <= 7 => "Normal",
                _ => "Fast"
            };

            SpeedDisplay.Text = $"Speed: {speedText}";
        }

        // Automatically scroll the text box at the defined speed
        private async void AutoScrollLiveDataOutput()
        {
            while (!_isDisposing)
            {
                await Task.Delay((int)(1000 / _scrollingSpeed)); // Adjust scrolling speed

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (LiveDataOutputTextBox != null && LiveDataOutputTextBox.IsLoaded)
                    {
                        LiveDataOutputTextBox.ScrollToEnd();
                    }
                });
            }
        }



    }
}
