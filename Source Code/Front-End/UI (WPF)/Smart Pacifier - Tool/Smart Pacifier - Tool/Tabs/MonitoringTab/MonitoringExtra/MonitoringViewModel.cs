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

        // Maps for storing grid and row references
        public Dictionary<PacifierItem, Grid> PacifierGridMap = new Dictionary<PacifierItem, Grid>();
        public Dictionary<Tuple<PacifierItem, SensorItem>, RowDefinition> SensorRowMap = new Dictionary<Tuple<PacifierItem, SensorItem>, RowDefinition>();

        // Dictionary mapping PacifierItem to its associated sensors
        public Dictionary<PacifierItem, ObservableCollection<SensorItem>> PacifierToSensorsMap = new Dictionary<PacifierItem, ObservableCollection<SensorItem>>();
        // Dictionary mapping SensorItem to its associated pacifiers
        public Dictionary<SensorItem, ObservableCollection<PacifierItem>> SensorToPacifiersMap = new Dictionary<SensorItem, ObservableCollection<PacifierItem>>();

        // Dictionary to hold LineChartGraph objects per sensor ID
        public Dictionary<string, Dictionary<string, LineChartGraph>> _lineChartGraphs = new Dictionary<string, Dictionary<string, LineChartGraph>>();

        // ObservableCollections to hold sensor types and plot types
        private ObservableCollection<string> _sensorTypes = new ObservableCollection<string>();
        private ObservableCollection<string> _plotTypes = new ObservableCollection<string>();
        private ObservableCollection<string> _sensorMeasurements = new ObservableCollection<string>();
        private ObservableCollection<string> _groupedSensorMeasurements = new ObservableCollection<string>();
        private ObservableCollection<string> _sensorValues = new ObservableCollection<string>();

        private Dictionary<string, ObservableCollection<string>> _groupedMeasurements = new Dictionary<string, ObservableCollection<string>>();

        // Dictionary to store real-time sensor data (as ObservableDictionary for binding)
        public Dictionary<string, SensorItem> _sensorDataDictionary = new Dictionary<string, SensorItem>();

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

        // Properties to bind sensorTypes and plotTypes to the UI
        public ObservableCollection<string> SensorTypes
        {
            get => _sensorTypes;
            set
            {
                _sensorTypes = value;
                OnPropertyChanged(nameof(SensorTypes));
            }
        }

        public ObservableCollection<string> PlotTypes
        {
            get => _plotTypes;
            set
            {
                _plotTypes = value;
                OnPropertyChanged(nameof(PlotTypes));
            }
        }

        // Properties to bind sensorMeasurements and plotTypes to the UI
        public ObservableCollection<string> SensorMeasurements
        {
            get => _sensorMeasurements;
            set
            {
                _sensorMeasurements = value;
                OnPropertyChanged(nameof(SensorMeasurements));
            }
        }

        // Properties to bind GroupedSensorMeasurements and plotTypes to the UI
        public ObservableCollection<string> GroupedSensorMeasurements
        {
            get => _groupedSensorMeasurements;
            set
            {
                _sensorMeasurements = value;
                OnPropertyChanged(nameof(GroupedSensorMeasurements));
            }
        }

        // Properties to bind GroupedSensorMeasurements and plotTypes to the UI
        public ObservableCollection<string> SensorValues
        {
            get => _sensorValues;
            set
            {
                _sensorValues = value;
                OnPropertyChanged(nameof(SensorValues));
            }
        }

        // Constructor initializing empty ObservableCollections
        public MonitoringViewModel()
        {
            PacifierItems = new ObservableCollection<PacifierItem>();
            SensorItems = new ObservableCollection<SensorItem>();
            SensorTypes = new ObservableCollection<string>();
            PlotTypes = new ObservableCollection<string>();
            SensorMeasurements = new ObservableCollection<string>();
            GroupedSensorMeasurements = new ObservableCollection<string>();
            _sensorDataDictionary = new Dictionary<string, SensorItem>();

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
        private void OnMessageReceived(object? sender, Broker.MessageReceivedEventArgs e)
        {
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

                        if (pacifierItem != null && pacifierItem.IsChecked)
                        {
                            Debug.WriteLine($"FirstOrDefault Pacifier_{pacifierItem.PacifierId}");
                            // Find or create the SensorItem for the given sensor type
                            var sensorItem = SensorItems.FirstOrDefault(s => s.SensorId == e.SensorType);

                            if (sensorItem == null)
                            {
                                Debug.WriteLine($"Add Sensor_{e.SensorType}");
                                // Add a new sensor item if it doesn't exist
                                sensorItem = new SensorItem(e.SensorType, pacifierItem);
                                pacifierItem.Sensors.Add(sensorItem);
                            }
                            else if (sensorItem.SensorIsChecked)
                            {
                                Debug.WriteLine($"Exists Sensor_{sensorItem.SensorId}");
                                if (!sensorItem.LinkedPacifiers.Contains(pacifierItem))
                                {
                                    Debug.WriteLine($"Linked Pacifier_{pacifierItem.PacifierId} to Sensor_{sensorItem.SensorId}");
                                    sensorItem.LinkedPacifiers.Add(pacifierItem);
                                }
                                Debug.WriteLine($"Selected Sensor_{e.SensorType}");

                                // Add the entire list of dictionaries to the SensorItem
                                sensorItem.MeasurementGroup.Clear();
                                sensorItem.MeasurementGroup = new ObservableCollection<Dictionary<string, object>>(e.ParsedData);

                                //Debug
                                //DisplaySensorDetails(pacifierItem, sensorItem);


                            }
                            else
                            {
                                Debug.WriteLine($"Is Not Checked: Sensor_{sensorItem.SensorId}");
                            }

                        }
                        else
                        {
                            // Optionally log if the pacifier item was not found or is not checked
                             Debug.WriteLine($"PacifierItem with ItemId {e.PacifierId} not found or not checked.");
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




        public void AddPacifierSensorPair(PacifierItem pacifierItem, SensorItem sensorItem)
        {
            // Add sensor to the list for this pacifier
            if (!PacifierToSensorsMap.ContainsKey(pacifierItem))
            {
                PacifierToSensorsMap[pacifierItem] = new ObservableCollection<SensorItem>();
            }
            if (!PacifierToSensorsMap[pacifierItem].Contains(sensorItem))
            {
                PacifierToSensorsMap[pacifierItem].Add(sensorItem);
            }

            // Add pacifier to the list for this sensor
            if (!SensorToPacifiersMap.ContainsKey(sensorItem))
            {
                SensorToPacifiersMap[sensorItem] = new ObservableCollection<PacifierItem>();
            }
            if (!SensorToPacifiersMap[sensorItem].Contains(pacifierItem))
            {
                SensorToPacifiersMap[sensorItem].Add(pacifierItem);
            }
        }

        private void OnSensorDataCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Only proceed if an existing value was replaced
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Debug.WriteLine("A value was replaced in _sensorDataDictionary.");
                UpdateGraphs();
            }
        }

        private void UpdateGraphs()
        {
            // This will be where you handle the graph updates after values change.
            Debug.WriteLine("UpdateGraphs called.");
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
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var grid = new Grid();
            grid.Children.Add(textBox);
            Content = grid;
        }
    }

}
