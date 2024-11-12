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
                            Debug.WriteLine($"Selected Pacifier_{pacifierItem.PacifierId}");
                            // Find or create the SensorItem for the given sensor type
                            var sensorItem = pacifierItem.Sensors.FirstOrDefault(s => s.SensorId == e.SensorType);

                            if (sensorItem == null)
                            {
                                Debug.WriteLine($"Add Sensor_{e.SensorType}");
                                // Add a new sensor item if it doesn't exist
                                sensorItem = new SensorItem(e.SensorType, pacifierItem);
                                pacifierItem.Sensors.Add(sensorItem);
                            }
                            else
                            {
                                Debug.WriteLine($"Exists Sensor_{sensorItem.SensorId}");

                                if (!sensorItem.LinkedPacifiers.Contains(pacifierItem))
                                {
                                    sensorItem.LinkedPacifiers.Add(pacifierItem);
                                }
                                

                                // Process each parsed data point
                                foreach (var kvp in e.ParsedData)
                                {

                                    if (sensorItem.SensorIsChecked)
                                    {
                                        Debug.WriteLine($"Selected Sensor_{e.SensorType}");

                                        var sensorGroupName = kvp.Key.Split('_')[0]; // Get the prefix before '_'

                                        // Find or create the SensorGroup for the sensor
                                        var sensorGroup = sensorItem.SensorGroups.FirstOrDefault(g => g.GroupName == sensorGroupName);
                                        if (sensorGroup == null)
                                        {
                                            Debug.WriteLine($"Add SensorGroup_{sensorGroupName}");
                                            // Add a new sensor group if it doesn't exist
                                            sensorGroup = new SensorGroup(sensorGroupName, sensorItem);
                                            sensorItem.SensorGroups.Add(sensorGroup);
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"Exists SensorGroup_{sensorGroupName}");
                                            // Ensure the MeasurementGroup for the sensor group is initialized
                                            if (sensorGroup.MeasurementGroup == null)
                                            {
                                                Debug.WriteLine($"Add MeasurementGroup_{kvp.Key} Value_{kvp.Value}");
                                                // Create a new MeasurementGroup if it doesn't exist
                                                sensorGroup.MeasurementGroup = new MeasurementGroup(sensorGroup.GroupName, sensorGroup);


                                            }

                                            Debug.WriteLine($"AddOrUpdate MeasurementGroup_{kvp.Key} Value_{kvp.Value}");
                                            // Update or add the measurement to the MeasurementGroup
                                            sensorGroup.MeasurementGroup.AddOrUpdateMeasurement(kvp.Key, Convert.ToDouble(kvp.Value));

                                        }

                                    }
                                }
                                
                                //AddPacifierSensorPair();




                                // Optionally, you could add debugging output here to log measurements, etc.
                                // Debug.WriteLine($"Updated measurement: {kvp.Key} with value {kvp.Value}");
                            }
                        }
                        else
                        {
                            // Optionally log if the pacifier item was not found or is not checked
                            // Debug.WriteLine($"PacifierItem with ItemId {e.PacifierId} not found or not checked.");
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
                // Debug.WriteLine($"Message skipped - PacifierId: {e.PacifierId} not in selected list or invalid data.");
            }
        }



        //private async Task DisplaySensorDetails(MeasurementGroup measurementGroup)
        //{
        //    if (measurementGroup == null)
        //    {
        //        Debug.WriteLine("Data not found.");
        //        return;
        //    }

        //    Debug.WriteLine($"PacifierId from Sensor Group: {measurementGroup.ParentSensorGroup.ParentSensorItem.ParentPacifierItem.ButtonText}");
        //    Debug.WriteLine($"SensorId from Sensor Group: {measurementGroup.ParentSensorGroup.ParentSensorItem.SensorId}");
        //    Debug.WriteLine($"GroupName: {measurementGroup.GroupName}");
        //    foreach (var measurement in measurementGroup.Measurements)
        //    {
        //        Debug.WriteLine($"Measurement Name: {measurement.Key}, Value: {measurement.Value:F2}");
        //    }

        //    foreach (var sensorItem in measurementGroup.ParentSensorGroup.ParentSensorItem.ParentPacifierItem.Sensors)
        //    {
        //        foreach (var pacifierItem in sensorItem.LinkedPacifiers)
        //        {
        //            Debug.WriteLine($"Sensor: {sensorItem.SensorId} - Pacifier: {pacifierItem.ItemId} ");
        //        }

        //    }
        //    // Display details about the pacifier (synchronous)
        //    //Debug.WriteLine($"PacifierId: {pacifierItem.ItemId}");
        //    //Debug.WriteLine($"Button Text: {pacifierItem.ButtonText}");
        //    //Debug.WriteLine($"IsChecked: {pacifierItem.IsChecked}");

        //    // Iterate over each sensor associated with this pacifier and display their details
        //    //foreach (var sensorItem in measurementGroup.ParentSensorItem.P)
        //    //{
        //    //    Debug.WriteLine($"PacifierId from Sensor: {sensorItem.ParentPacifierItem.ItemId}");
        //    //    Debug.WriteLine($"SensorId: {sensorItem.SensorId}");


        //    //    // Iterate through sensor groups (if any) and display their details
        //    //    foreach (var sensorGroup in sensorItem.SensorGroups)
        //    //    {
        //    //        //Debug.WriteLine($"PacifierId from Sensor Group: {sensorGroup.ParentSensorItem.ParentPacifierItem.ButtonText}");
        //    //        //Debug.WriteLine($"SensorId from Sensor Group: {sensorGroup.ParentSensorItem.SensorId}");
        //    //        Debug.WriteLine($"GroupName: {sensorGroup.GroupName}");

        //    //        // Access the MeasurementGroup from SensorGroup (asynchronous, if required)
        //    //        if (sensorGroup.MeasurementGroup != null)
        //    //        {
        //    //            if (!string.IsNullOrEmpty(sensorGroup.MeasurementGroup.GroupName))
        //    //            {
        //    //                Debug.WriteLine($"Measurement Group Name: {sensorGroup.MeasurementGroup.GroupName}");
        //    //            }
        //    //            else
        //    //            {
        //    //                Debug.WriteLine("MeasurementGroup exists, but GroupName is null or empty.");
        //    //            }

        //    //            // Iterate over the measurements in the MeasurementGroup (asynchronous if needed)
        //    //            foreach (var measurement in sensorGroup.MeasurementGroup.Measurements)
        //    //            {
        //    //                Debug.WriteLine($"Measurement Name: {measurement.Key}, Value: {measurement.Value:F2}");
        //    //            }
        //    //        }
        //    //        else
        //    //        {
        //    //            Debug.WriteLine("No MeasurementGroup available in this sensor group.");
        //    //        }
        //    //    }
        //    //}

        //    // Ensure all tasks are completed before continuing or closing
        //    await Task.CompletedTask;
        //}


        private void DisplaySensorDetails(PacifierItem pacifierItem)
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

            foreach (var sensorItem in pacifierItem.Sensors)
            {
                detailsBuilder.AppendLine($"SensorId: {sensorItem.SensorId}");
                detailsBuilder.AppendLine($"PacifierId from Sensor: {sensorItem.ParentPacifierItem.PacifierId}");

                foreach (var sensorGroup in sensorItem.SensorGroups)
                {
                    detailsBuilder.AppendLine($"GroupName: {sensorGroup.GroupName}");
                    detailsBuilder.AppendLine($"SensorId from Sensor Group: {sensorGroup.ParentSensorItem.SensorId}");
                    detailsBuilder.AppendLine($"PacifierId from Sensor Group: {sensorGroup.ParentSensorItem.ParentPacifierItem.PacifierId}");

                    if (sensorGroup.MeasurementGroup != null)
                    {
                        if (!string.IsNullOrEmpty(sensorGroup.MeasurementGroup.GroupName))
                        {
                            detailsBuilder.AppendLine($"Measurement Group Name: {sensorGroup.MeasurementGroup.GroupName}");
                        }
                        else
                        {
                            detailsBuilder.AppendLine("MeasurementGroup exists, but GroupName is null or empty.");
                        }

                        foreach (var measurement in sensorGroup.MeasurementGroup.Measurements)
                        {
                            detailsBuilder.AppendLine($"Measurement Name: {measurement.Key}, Value: {measurement.Value:F2}");
                        }
                    }
                    else
                    {
                        detailsBuilder.AppendLine("No MeasurementGroup available in this sensor group.");
                    }
                    detailsBuilder.AppendLine();
                }
                detailsBuilder.AppendLine();
            }

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
            bool toggledPacifiers = PacifierItems.Any(p => p.IsChecked);
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

        //// Handle toggle changes for pacifiers or sensors
        //public void HandleToggleChanged(object sender, EventArgs e)
        //{
        //    if (sender is PacifierItem pacifierItem)
        //    {
        //        if (pacifierItem.IsChecked)
        //        {
        //            if (pacifierItem.Type == PacifierItem.ItemType.Pacifier && !checkedPacifiers.Contains(pacifierItem))
        //            {
        //                checkedPacifiers.Add(pacifierItem);
        //            }
        //            else if (pacifierItem.Type == PacifierItem.ItemType.Sensor && !checkedSensors.Contains(pacifierItem))
        //            {
        //                checkedSensors.Add(pacifierItem);
        //            }
        //        }
        //        else
        //        {
        //            if (pacifierItem.Type == PacifierItem.ItemType.Pacifier && checkedPacifiers.Contains(pacifierItem))
        //            {
        //                checkedPacifiers.Remove(pacifierItem);
        //            }
        //            else if (pacifierItem.Type == PacifierItem.ItemType.Sensor && checkedSensors.Contains(pacifierItem))
        //            {
        //                checkedSensors.Remove(pacifierItem);
        //            }
        //        }
        //    }
        //}

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
