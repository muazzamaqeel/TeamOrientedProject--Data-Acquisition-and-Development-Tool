using Smart_Pacifier___Tool.Components;
using SmartPacifier.BackEnd.CommunicationLayer.MQTT;
using SmartPacifier.BackEnd.CommunicationLayer.Protobuf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using static Smart_Pacifier___Tool.Components.LineChartGraph;
using static SmartPacifier.BackEnd.CommunicationLayer.MQTT.Broker;
using System.Windows.Controls;
using static Smart_Pacifier___Tool.Components.PacifierItem;
using System.Runtime.Intrinsics.X86;
using System.IO.Packaging;
using OxyPlot.Series;
using OxyPlot;
using OxyPlot.Axes;
using SmartPacifier.Interface.Services;
using InfluxDB.Client.Api.Domain;
using System.Windows.Media;

namespace Smart_Pacifier___Tool.Tabs.MonitoringTab.MonitoringExtra
{
    public class MonitoringViewModel : INotifyPropertyChanged
    {
        // ObservableCollections to bind to UI for Pacifiers and Sensors
        public ObservableCollection<PacifierItem> _pacifierItems = new ObservableCollection<PacifierItem>();
        public ObservableCollection<SensorItem> _sensorItems = new ObservableCollection<SensorItem>();

        // Memorizes the order for toggling buttons
        public ObservableCollection<PacifierItem> _checkedPacifierItems = new ObservableCollection<PacifierItem>();
        public ObservableCollection<SensorItem> _checkedSensorItems = new ObservableCollection<SensorItem>();

        public Dictionary<string, int> SensorIntervals { get; private set; } = new Dictionary<string, int>();

        public Dictionary<string, DateTime> _lastUpdateTimestamps = new Dictionary<string, DateTime>();

        // Maps for storing grid and row references
        public Dictionary<PacifierItem, Grid> PacifierGridMap = new Dictionary<PacifierItem, Grid>();
        public Dictionary<Tuple<PacifierItem, SensorItem>, RowDefinition> SensorRowMap = new Dictionary<Tuple<PacifierItem, SensorItem>, RowDefinition>();

        public Dictionary<PacifierItem, DateTime> _lastPacifierUpdate = new Dictionary<PacifierItem, DateTime>();

        private Dictionary<PacifierItem, CancellationTokenSource> _pacifierCancellationTokens = new Dictionary<PacifierItem, CancellationTokenSource>();


        private readonly ILineProtocol _lineProtocol;
        private string _currentCampaignName;
        private bool _isCampaignActive = true;


        public string CurrentCampaignName
        {
            get => _currentCampaignName;
            set
            {
                _currentCampaignName = value;
                OnPropertyChanged(nameof(CurrentCampaignName));
            }
        }

        public ILineProtocol LineProtocolService => _lineProtocol;

        public bool IsCampaignActive
        {
            get => _isCampaignActive;
            set
            {
                _isCampaignActive = value;
                OnPropertyChanged(nameof(IsCampaignActive));
            }
        }


        // Properties to expose ObservableCollections for Pacifier and Sensor items
        public ObservableCollection<PacifierItem> PacifierItems
        {
            get => _pacifierItems;
            set
            {
                _pacifierItems = value;
                OnPropertyChanged(nameof(PacifierItems));
            }
        }

        public ObservableCollection<SensorItem> SensorItems
        {
            get => _sensorItems;
            set
            {
                _sensorItems = value;
                OnPropertyChanged(nameof(SensorItems));
            }
        }

        public ObservableCollection<PacifierItem> CheckedPacifierItems
        {
            get => _checkedPacifierItems;
            set
            {
                _checkedPacifierItems = value;
                OnPropertyChanged(nameof(PacifierItems));
            }
        }

        public ObservableCollection<SensorItem> CheckedSensorItems
        {
            get => _checkedSensorItems;
            set
            {
                _checkedSensorItems = value;
                OnPropertyChanged(nameof(CheckedSensorItems));
            }
        }


        // Constructor initializing empty ObservableCollections
        public MonitoringViewModel(ILineProtocol lineProtocol, string currentCampaignName)
        {


            _lineProtocol = lineProtocol ?? throw new ArgumentNullException(nameof(lineProtocol));
            _currentCampaignName = currentCampaignName;

            PacifierItems = new ObservableCollection<PacifierItem>();
            SensorItems = new ObservableCollection<SensorItem>();

            // Receive real-time data from the broker
            SubscribeToBroker();
        }

        /// <summary>
        /// Subscribe to the broker to receive messages
        /// </summary>
        private void SubscribeToBroker()
        {
            Broker.Instance.MessageReceived += OnMessageReceived;
            Debug.WriteLine("MonitoringVM: Subscribed to broker messages.");
        }

        /// <summary>
        /// Handle incoming messages from the broker
        /// </summary>
        public void OnMessageReceived(object? sender, Broker.MessageReceivedEventArgs e)
        {

            if (!IsCampaignActive)
            {
                Debug.WriteLine("Campaign has ended. Ignoring incoming messages.");
                return;
            }

            // Get the current date and time
            DateTime dateTime = DateTime.Now;
            string entryTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

            // Only proceed if the pacifier ID is in the checkedPacifiers list and data is valid
            if (PacifierItems.Any(p => p.PacifierId == e.PacifierId) && e.SensorType != null && e.ParsedData != null)
            {
                try
                {
                    // Use Dispatcher to safely update the ObservableCollection on the UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Find the PacifierItem in PacifierItems with a matching ItemId and also in checkedPacifiers
                        var pacifierItem = PacifierItems.FirstOrDefault(p => p.PacifierId == e.PacifierId);
                        var pacifiercount = PacifierItems.Count;

                        if (pacifierItem != null)
                        {
                            // Cancel any existing timeout for this pacifier
                            if (_pacifierCancellationTokens.ContainsKey(pacifierItem))
                            {
                                _pacifierCancellationTokens[pacifierItem].Cancel();
                                _pacifierCancellationTokens.Remove(pacifierItem);
                            }

                            // Reset the status and start the 5-second check asynchronously
                            pacifierItem.Status = "Receiving";
                            _pacifierCancellationTokens[pacifierItem] = new CancellationTokenSource();

                            // Start the async task to check if there were no updates for 5 seconds
                            _ = CheckForTimeout(pacifierItem, _pacifierCancellationTokens[pacifierItem].Token);

                            // Convert ObservableCollection to List before passing to AppendToCampaignFile
                            _lineProtocol.AppendToCampaignFile(
                                _currentCampaignName,
                                pacifiercount,
                                pacifierItem.ButtonText,
                                e.SensorType,
                                e.ParsedData.ToList(), // Convert to List here
                                entryTime);
                            //MessageBox.Show($"Data appended to campaign file for Pacifier: {pacifierItem.PacifierId}, Sensor: {e.SensorType}");

                            string uniqueKey = $"{pacifierItem.PacifierId}_{e.SensorType}";

                            //Check if we should throttle based on pacifier's update frequency
                            if (_lastUpdateTimestamps.ContainsKey(uniqueKey))
                            {
                                var lastUpdate = _lastUpdateTimestamps[uniqueKey];
                                var timeDifference = (DateTime.Now - lastUpdate).TotalMilliseconds;

                                // If the time difference is less than the update frequency, skip this update
                                if (timeDifference < pacifierItem.UpdateFrequency)
                                {
                                    Debug.WriteLine($"Throttle: Skipping update for Pacifier {pacifierItem.PacifierId} and Sensor {e.SensorType}. Time difference: {timeDifference}ms");
                                    return; // Skip the update
                                }
                            }

                            // Update the timestamp for the pacifier and sensor pair
                            _lastUpdateTimestamps[uniqueKey] = DateTime.Now;

                            if (pacifierItem.IsChecked)
                            {
                                //Debug.WriteLine($"FirstOrDefault Pacifier_{pacifierItem.PacifierId}");
                                // Find or create the SensorItem for the given sensor type
                                var sensorItem = SensorItems.FirstOrDefault(s => s.SensorId == e.SensorType);

                                if (sensorItem == null)
                                {
                                    //Debug.WriteLine($"Add Sensor_{e.SensorType}");
                                    // Add a new sensor item if it doesn't exist
                                    sensorItem = new SensorItem(e.SensorType, pacifierItem);
                                    sensorItem.dateTime = dateTime;
                                    pacifierItem.Sensors.Add(sensorItem);

                                    foreach (var dictionary in e.ParsedData)
                                    {
                                        if (dictionary.ContainsKey("sensorGroup"))
                                        {
                                            if (!SensorIntervals.ContainsKey(dictionary["sensorGroup"].ToString()))
                                            {
                                                SensorIntervals.Add(dictionary["sensorGroup"].ToString(), 10);
                                                Debug.WriteLine($"Added sensorGroup {dictionary["sensorGroup"]} with interval 10");

                                            }
                                            // Check for uniqueness
                                            if (!sensorItem.SensorGroups.Contains(dictionary["sensorGroup"].ToString()))
                                            {
                                                sensorItem.SensorGroups.Add(dictionary["sensorGroup"].ToString());
                                            }
                                        }
                                    }


                                }
                                else
                                {
                                    sensorItem.dateTime = dateTime;
                                    if (sensorItem.SensorIsChecked)
                                    {
                                        pacifierItem.RawData.Clear();
                                        if (e.Payload != null) pacifierItem.RawData.Add(e.Payload);

                                        //Debug.WriteLine($"Exists Sensor_{sensorItem.SensorId}");
                                        if (!sensorItem.LinkedPacifiers.Contains(pacifierItem))
                                        {
                                            //Debug.WriteLine($"Linked Pacifier_{pacifierItem.PacifierId} to Sensor_{sensorItem.SensorId}");
                                            sensorItem.LinkedPacifiers.Add(pacifierItem);
                                        }
                                        //Debug.WriteLine($"Selected Sensor_{e.SensorType}");

                                        // Add the entire list of dictionaries to the SensorItem
                                        sensorItem.MeasurementGroup.Clear();
                                        sensorItem.MeasurementGroup = new ObservableCollection<Dictionary<string, object>>(e.ParsedData);

                                        AddLineSeries(pacifierItem, sensorItem);
                                        //Debug
                                        //DisplaySensorDetails(pacifierItem, sensorItem);
                                    }

                                }


                            }
                            else
                            {
                                // Optionally log if the pacifier item was not found or is not checked
                                //Debug.WriteLine($"PacifierItem with ItemId {e.PacifierId} not found or not checked.");
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    // Log any errors that occur during processing
                    Debug.WriteLine($"Error processing message: {ex.Message}");
                }
            }
            else
            {
                // Debug log for skipped messages
                //Debug.WriteLine($"Message skipped - PacifierId: {e.PacifierId} not in selected list or invalid data.");
            }
        }
        private async Task CheckForTimeout(PacifierItem pacifierItem, CancellationToken token)
        {
            //try
            //{
                // Wait for 5 seconds or until the cancellation token is triggered
                await Task.Delay(5000, token);

                pacifierItem.Status = "Not Receiving";
                pacifierItem.StatusColor = Brushes.Red; // Changes the circle to red


            // If we reach here, it means 5 seconds passed without a new message

            //    //Debug.WriteLine($"No updates received for Pacifier {pacifierItem.PacifierId} in the last 5 seconds.");
            //}
            //catch (TaskCanceledException)
            //{
            //    // Task was canceled, meaning a new message arrived before the timeout
            //    //Debug.WriteLine($"Update received for Pacifier {pacifierItem.PacifierId}, status reset.");
            //}
        }

        private void AddLineSeries(PacifierItem pacifierItem, SensorItem sensorItem)
        {

            foreach (var measurementGraph in sensorItem.SensorGraphs)
            {
                //Debug.WriteLine($"AddLineSeries for Sensor {sensorItem.SensorId} to Graph {measurementGraph.Uid}");
                AddDataToGraphs(measurementGraph, sensorItem, pacifierItem);
            }
        }



        private void AddDataToGraphs(LineChartGraph measurementGraph, SensorItem sensorItem, PacifierItem pacifierItem)
        {

            foreach (var sensorGroup in sensorItem.MeasurementGroup)
            {
                var firstKvp = sensorGroup.FirstOrDefault();
                string uniquePlotId = $"{sensorItem.SensorId}_{firstKvp.Value}_{pacifierItem.PacifierId}";

                if (measurementGraph.PlotId == uniquePlotId)
                {
                    //if (_lastUpdateTimestamps.ContainsKey(uniquePlotId))
                    //{
                    //    var lastUpdate = _lastUpdateTimestamps[uniquePlotId];
                    //    var timeDifference = (DateTime.Now - lastUpdate).TotalMilliseconds;

                    //    // If the time difference is less than the update frequency, skip this update
                    //    if (timeDifference < pacifierItem.UpdateFrequency)
                    //    {
                    //        Debug.WriteLine($"Throttle: Skipping update for Pacifier {pacifierItem.PacifierId} and Sensor {sensorItem.SensorId}. Time difference: {timeDifference}ms");
                    //        return; // Skip the update
                    //    }
                    //}

                    //// Update the timestamp for the pacifier and sensor pair
                    //_lastUpdateTimestamps[uniquePlotId] = DateTime.Now;

                    foreach (var kvp in sensorGroup)
                    {
                        if (kvp.Key == "sensorGroup") continue; // Skip the sensorGroup field

                        // Find the existing LineSeries for this sensor group (or create a new one if not found)
                        var existingSeries = measurementGraph.LineSeriesCollection.FirstOrDefault(series => series.Title == kvp.Key);

                        OxyColor[] blueShades = new OxyColor[]
                        {
                            OxyColor.FromRgb(0, 0, 255),    // Pure Blue
                            OxyColor.FromRgb(90, 90, 255),  // Even lighter Blue
                            OxyColor.FromRgb(255, 255, 255),  // Light Blue
                            OxyColor.FromRgb(120, 120, 255), // Very light Blue
                            OxyColor.FromRgb(60, 60, 255)  // Lighter Blue
                        };

                        if (existingSeries == null)
                        {
                            existingSeries = new LineSeries
                            {
                                Title = kvp.Key,
                                MarkerType = MarkerType.None,
                                MarkerSize = 2,
                                Color = blueShades[measurementGraph.LineSeriesCollection.Count % blueShades.Length]
                            };
                            measurementGraph.LineSeriesCollection.Add(existingSeries);
                            measurementGraph.PlotModel.Series.Add(existingSeries);
                        }

                        DateTime xValue = sensorItem.dateTime;
                        double yValue = Convert.ToDouble(kvp.Value);

                        existingSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(xValue), yValue));

                        // Enforce point limit based on sensor intervals
                        if (existingSeries.Points.Count > SensorIntervals[measurementGraph.Name])
                        {
                            int difference = existingSeries.Points.Count - SensorIntervals[measurementGraph.Name];
                            for (int i = 0; i <= difference; i++)
                            {
                                existingSeries.Points.RemoveAt(0);
                            }
                        }
                    }

                    measurementGraph.PlotModel.IsLegendVisible = true;
                    measurementGraph.PlotModel.InvalidatePlot(true);
                }
            }
        }

        // Property changed notification
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }


}
