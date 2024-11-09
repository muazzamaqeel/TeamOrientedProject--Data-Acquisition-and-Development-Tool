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

namespace Smart_Pacifier___Tool.Tabs.MonitoringTab.MonitoringExtra
{
    public class MonitoringViewModel : INotifyPropertyChanged
    {
        // ObservableCollections to bind to UI for Pacifiers and Sensors
        private ObservableCollection<PacifierItem> _pacifierItems = new ObservableCollection<PacifierItem>();
        private ObservableCollection<PacifierItem> _sensorItems = new ObservableCollection<PacifierItem>();

        // Lists to track checked pacifiers and sensors
        public ObservableCollection<PacifierItem> checkedPacifiers { get; set; } = new ObservableCollection<PacifierItem>();
        public ObservableCollection<PacifierItem> checkedSensors { get; set; } = new ObservableCollection<PacifierItem>();

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
        public Dictionary<string, Sensor> _sensorDataDictionary = new Dictionary<string, Sensor>();

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

        public ObservableCollection<PacifierItem> SensorItems
        {
            get => _sensorItems;
            set
            {
                _sensorItems = value;
                OnPropertyChanged(nameof(SensorItems));
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
            SensorItems = new ObservableCollection<PacifierItem>();
            SensorTypes = new ObservableCollection<string>();
            PlotTypes = new ObservableCollection<string>();
            SensorMeasurements = new ObservableCollection<string>();
            GroupedSensorMeasurements = new ObservableCollection<string>();
            _sensorDataDictionary = new Dictionary<string, Sensor>();

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
            if (checkedPacifiers.Any(p => p.ItemId == e.PacifierId) && e.SensorType != null && e.ParsedData != null)
            {
                try
                {
                    // Use Dispatcher to safely update the ObservableCollection on the UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Find the PacifierItem corresponding to the PacifierId
                        var pacifierItem = checkedPacifiers.FirstOrDefault(p => p.ItemId == e.PacifierId);

                        if (pacifierItem != null)
                        {
                            // Store the sensor type directly in the dictionary under a specific sensor ID or type
                            if (!_sensorDataDictionary.ContainsKey(e.SensorType))
                            {
                                _sensorDataDictionary[e.SensorType] = new Sensor(e.SensorType); // Initialize Sensor object if not present
                            }

                            var sensor = _sensorDataDictionary[e.SensorType];

                            // Process each parsed data point
                            foreach (var kvp in e.ParsedData)
                            {
                                var sensorGroup = kvp.Key.Split('_')[0]; // Get the prefix before '_'

                                // Check if the sensor group exists, if not, add it
                                if (!sensor.SensorGroup.Types.Contains(sensorGroup))
                                {
                                    sensor.SensorGroup.Add(sensorGroup); // Add the sensor group to the SensorGroup's Types list
                                }

                                // Ensure the MeasurementGroup for the sensor is properly initialized for this sensorGroup
                                if (sensor.MeasurementGroup == null || sensor.MeasurementGroup.GroupName != sensorGroup)
                                {
                                    // Create a new MeasurementGroup for the specific sensorGroup if it doesn't exist
                                    sensor.MeasurementGroup = new MeasurementGroup(sensorGroup); // Initialize MeasurementGroup with group name
                                }

                                // Ensure that the GroupName is properly set when the MeasurementGroup is initialized
                                if (string.IsNullOrEmpty(sensor.MeasurementGroup.GroupName))
                                {
                                    sensor.MeasurementGroup.GroupName = sensorGroup; // Set the group name
                                }

                                // Check if the measurement already exists in the MeasurementGroup
                                if (!sensor.MeasurementGroup.ContainsMeasurement(kvp.Key))
                                {
                                    // Add the measurement to the specific group (ensure group name is always set)
                                    sensor.MeasurementGroup.AddMeasurement(kvp.Key, Convert.ToDouble(kvp.Value));
                                }
                                else
                                {
                                    // Optionally update the existing measurement here if needed
                                    sensor.MeasurementGroup.UpdateMeasurement(kvp.Key, Convert.ToDouble(kvp.Value));
                                }

                                // Check if the sensor is already in the Sensors collection for the PacifierItem
                                var existingSensor = pacifierItem.Sensors.FirstOrDefault(s => s.SensorId == sensor.SensorId);
                                if (existingSensor == null)
                                {
                                    // Add the new sensor to the Sensors collection
                                    pacifierItem.Sensors.Add(sensor);
                                }
                                else
                                {
                                    // Update existing sensor details as needed (you can update measurements here if necessary)
                                    existingSensor.MeasurementGroup = sensor.MeasurementGroup;
                                }

                                // Display the updated sensor details
                                DisplaySensorDetails(sensor);
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MonitoringVM: Error processing message: {ex.Message}");
                }
            }
            else
            {
                //Debug.WriteLine($"MonitoringVM: Message skipped - PacifierId: {e.PacifierId} not in checked list, or invalid data.");
            }
        }

        private void DisplaySensorDetails(Sensor sensor)
        {

            var pacifierItem = checkedPacifiers.FirstOrDefault(p => p.ItemId == sensor.SensorId); // Example: matching PacifierId to SensorId

            if (pacifierItem != null)
            {
                // Display details about the associated pacifier
                Debug.WriteLine($"PacifierId: {pacifierItem.PacifierId}");
                Debug.WriteLine($"Button Text: {pacifierItem.ButtonText}");
                Debug.WriteLine($"IsChecked: {pacifierItem.IsChecked}");
            }
            else
            {
                Debug.WriteLine("Pacifier not found for the sensor.");
            }

            // Now display the sensor details
            Debug.WriteLine($"SensorType: {sensor.SensorId}");
            Debug.WriteLine($"Sensor Groups: {string.Join(", ", sensor.SensorGroup.Types)}");

            if (sensor.MeasurementGroup != null)
            {
                // Display the GroupName, which should now be properly set
                Debug.WriteLine($"Measurement Group Name: {sensor.MeasurementGroup.GroupName}");

                foreach (var measurement in sensor.MeasurementGroup.Measurements)
                {
                    Debug.WriteLine($"Measurement Name: {measurement.Name}, Value: {measurement.Value:F2}");
                }
            }
            else
            {
                Debug.WriteLine("No measurements available.");
            }
        }



        //private void TrackValueChanges()
        //{
        //    // Unsubscribe from previous handlers to avoid duplication
        //    foreach (var collection in _sensorDataDictionary.Values)
        //    {
        //        collection.CollectionChanged -= OnSensorDataCollectionChanged;
        //    }

        //    // Subscribe to each collection's CollectionChanged event
        //    foreach (var collection in _sensorDataDictionary.Values)
        //    {
        //        string values = string.Join(", ", collection.Select(v => v.ToString("F2")));
        //        Debug.WriteLine($"Values Changed: [{values}]");
        //        collection.CollectionChanged += OnSensorDataCollectionChanged;
        //    }
        //}

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
            bool toggledPacifiers = checkedPacifiers.Count > 0;

            // Make all SensorItems visible if pacifier item is toggled on
            foreach (var sensorItem in SensorItems)
            {
                sensorItem.Visibility = toggledPacifiers ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void GroupMeasurements(ObservableCollection<string> sensorMeasurements)
        {
            //// Clear existing group and prefix lists before processing
            //GroupedSensorMeasurements.Clear();
            //GroupedMeasurements.Clear();

            //var groupedMeasurements = sensorMeasurements
            //    .GroupBy(m => m.Split('_')[0]) // Extract prefix (before the first '_')
            //    .ToList();

            //// Process each group
            //foreach (var group in groupedMeasurements)
            //{
            //    // Add the prefix to GroupedSensorMeasurements if it isn't already added
            //    if (!GroupedSensorMeasurements.Contains(group.Key))
            //    {
            //        GroupedSensorMeasurements.Add(group.Key);
            //    }

            //    // Store the measurements in GroupedMeasurements by prefix
            //    GroupedMeasurements[group.Key] = new ObservableCollection<string>(group);
            //}
        }

        // Handle toggle changes for pacifiers or sensors
        public void HandleToggleChanged(object sender, EventArgs e)
        {
            if (sender is PacifierItem pacifierItem)
            {
                if (pacifierItem.IsChecked)
                {
                    if (pacifierItem.Type == PacifierItem.ItemType.Pacifier && !checkedPacifiers.Contains(pacifierItem))
                    {
                        checkedPacifiers.Add(pacifierItem);
                    }
                    else if (pacifierItem.Type == PacifierItem.ItemType.Sensor && !checkedSensors.Contains(pacifierItem))
                    {
                        checkedSensors.Add(pacifierItem);
                    }
                }
                else
                {
                    if (pacifierItem.Type == PacifierItem.ItemType.Pacifier && checkedPacifiers.Contains(pacifierItem))
                    {
                        checkedPacifiers.Remove(pacifierItem);
                    }
                    else if (pacifierItem.Type == PacifierItem.ItemType.Sensor && checkedSensors.Contains(pacifierItem))
                    {
                        checkedSensors.Remove(pacifierItem);
                    }
                }
            }
        }

        // Property changed notification
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
