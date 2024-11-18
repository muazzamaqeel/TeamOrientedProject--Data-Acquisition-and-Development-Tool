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
            string entryTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss");

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

                        if (pacifierItem != null)
                        {
                            // Convert ObservableCollection to List before passing to AppendToCampaignFile
                            _lineProtocol.AppendToCampaignFile(
                                _currentCampaignName,
                                pacifierItem.ButtonText,
                                e.SensorType,
                                e.ParsedData.ToList(), // Convert to List here
                                entryTime);
                            //MessageBox.Show($"Data appended to campaign file for Pacifier: {pacifierItem.PacifierId}, Sensor: {e.SensorType}");


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

                //Debug.WriteLine($"AddDataToGraphs Graph ID {measurementGraph.PlotId} is this {uniquePlotId}");

                if (measurementGraph.PlotId == uniquePlotId)
                {
                    // Now, add the remaining key-value pairs as DataPoints to the corresponding LineSeries
                    foreach (var kvp in sensorGroup)
                    {
                        if (kvp.Key == "sensorGroup") continue; // Skip the sensorGroup field

                        // Find the existing LineSeries for this sensor group (or create a new one if not found)
                        var existingSeries = measurementGraph.LineSeriesCollection.FirstOrDefault(series => series.Title == kvp.Key);

                        if (existingSeries == null)
                        {
                            // Create a new LineSeries for this sensor group if it doesn't already exist
                            existingSeries = new LineSeries
                            {
                                Title = kvp.Key,
                                MarkerType = MarkerType.Square
                            };
                            measurementGraph.LineSeriesCollection.Add(existingSeries);
                            measurementGraph.PlotModel.Series.Add(existingSeries);

                            //Debug.WriteLine($"AddDataToGraphs Created New Line Series {kvp.Key}");
                        }

                        //Debug.WriteLine($"AddDataToGraphs Line Series {kvp.Key} with Interval {measurementGraph.Interval}");

                        // Add the value as DataPoint to the existing LineSeries for this sensor group
                        DateTime xValue = sensorItem.dateTime;
                        double yValue = Convert.ToDouble(kvp.Value); // Assuming the value can be converted to double

                        existingSeries.Points.Add((new DataPoint(DateTimeAxis.ToDouble(xValue), yValue)));

                        if (existingSeries.Points.Count > SensorIntervals[measurementGraph.Name])
                        {
                            //Debug.WriteLine($"Update Graph Interval: {SensorIntervals[measurementGraph.Name]}");

                            var difference = existingSeries.Points.Count - SensorIntervals[measurementGraph.Name];
                            for (int i = 0; i <= difference; i++)
                            {
                                existingSeries.Points.RemoveAt(0);
                            }
                        }
                        
                        //Debug.WriteLine($"AddDataToGraphs Add DataPoints {yValue}");
                    }

                }

            }

            measurementGraph.PlotModel.IsLegendVisible = true;
            measurementGraph.PlotModel.InvalidatePlot(true);


        }


        private void DisplaySensorDetails(PacifierItem pacifierItem, SensorItem sensorItem)
        {
            if (pacifierItem == null)
            {
                MessageBox.Show("Pacifier item not found.", "Sensor Details", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }


            StringBuilder detailsBuilder = new StringBuilder();

            // Collect sensor details
            detailsBuilder.AppendLine($"PacifierId: {pacifierItem.PacifierId}");
            detailsBuilder.AppendLine($"Button Text: {pacifierItem.ButtonText}");
            detailsBuilder.AppendLine($"IsChecked: {pacifierItem.IsChecked}");
            detailsBuilder.AppendLine();
            detailsBuilder.AppendLine($"SensorId: {sensorItem.SensorId}");

            foreach (var sensorGroup in sensorItem.MeasurementGroup)
            {
                if (sensorGroup != null)
                {
                    foreach (var measurement in sensorGroup)
                    {
                        detailsBuilder.AppendLine($"Measurement Name: {measurement.Key}, Value: {measurement.Value}");
                    }
                }
                else
                {
                    detailsBuilder.AppendLine("No MeasurementGroup available in this sensor.");
                }
                detailsBuilder.AppendLine();
            }
            detailsBuilder.AppendLine();

            // Open the custom window to display the details asynchronously
            var sensorDetailsWindow = new SensorDetailsWindow(detailsBuilder.ToString());
            sensorDetailsWindow.Show();
        }

        public void TogglePacifierVisibility(PacifierItem pacifierItem)
        {
            bool toggledPacifiers = PacifierItems.Any(p => p.IsChecked);
            // Make all SensorItems visible if pacifier item is toggled on
            foreach (var sensorItem in SensorItems)
            {
                sensorItem.Visibility = toggledPacifiers ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // Property changed notification
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }


    public class SensorDetailsWindow : Window
    {
        public SensorDetailsWindow(string details)
        {
            Title = "Sensor Details";
            Width = 400;
            Height = 300;

            var textBox = new TextBox
            {
                Text = details,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
            };

            var grid = new Grid();
            grid.Children.Add(textBox);
            Content = grid;
        }
    }

}
