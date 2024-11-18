using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Smart_Pacifier___Tool.Components; // Assuming your SensorItem and MeasurementGroup classes are in this namespace

namespace Smart_Pacifier___Tool.Tabs.MonitoringTab
{
    public partial class RawDataView : UserControl, INotifyPropertyChanged
    {
        private UserControl backLocation;
        private bool activeMonitoring;
        public PacifierItem PacifierItemT;
        public string PacifierName { get; private set; }
        public bool ActiveMonitoring
        {
            get => activeMonitoring;
            set
            {
                if (activeMonitoring != value)
                {
                    activeMonitoring = value;
                    OnPropertyChanged(nameof(ActiveMonitoring));
                }
            }
        }

        public ObservableCollection<SensorData> SensorEntries { get; set; } = new ObservableCollection<SensorData>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Initializes a new instance of the Raw Data view, with a table.
        /// </summary>
        /// <param name="pacifierItem">PacifierItem to load sensors from.</param>
        /// <param name="backLocation">The back location.</param>
        /// <param name="activeMonitoring">Set to true if active Monitoring otherwise to false.</param>
        public RawDataView(PacifierItem pacifierItem, UserControl backLocation, bool activeMonitoring)
        {
            InitializeComponent();
            DataContext = this;
            PacifierItemT = pacifierItem;
            PacifierName = $"Pacifier {pacifierItem.PacifierId}";
            this.backLocation = backLocation;
            ActiveMonitoring = !activeMonitoring;

            // Initialize CollectionViewSource for sorting
            var collectionViewSource = new CollectionViewSource
            {
                Source = SensorEntries
            };

            // Set the sorting order by Timestamp, descending
            collectionViewSource.SortDescriptions.Add(new SortDescription("Timestamp", ListSortDirection.Descending));
            RawDataTable.ItemsSource = collectionViewSource.View;


            LoadData(pacifierItem);

            pacifierItem.RawData.CollectionChanged += OnMeasurementGroupUpdated;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var parent = this.Parent as ContentControl;
            if (parent != null)
            {
                parent.Content = backLocation;
            }
        }

        // Loads data dynamically from PacifierItem and its sensors
        private void LoadData(PacifierItem pacifierItem)
        {
            SensorEntries.Clear(); // Clear previous data before adding new rows

            // Clear existing columns (optional, to avoid duplicate columns)
            RawDataTable.Columns.Clear();

            // Add the Timestamp and Sensor columns
            RawDataTable.Columns.Add(new DataGridTextColumn
            {
                Header = "Timestamp",
                Binding = new Binding("Timestamp"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            RawDataTable.Columns.Add(new DataGridTextColumn
            {
                Header = "Sensor",
                Binding = new Binding("Sensor"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
            });

            // Loop through the Sensors in the PacifierItem
            foreach (var sensorItem in pacifierItem.Sensors)
            {
                // Iterate over each sensor's MeasurementGroups (assuming it's a dictionary)
                foreach (var measurementGroup in sensorItem.MeasurementGroup)
                {
                    // Loop through the keys in the measurement group (dynamic columns)
                    foreach (var key in measurementGroup.Keys)
                    {
                        // Retrieve the value of "sensorGroup" from the measurementGroup dictionary
                        if (measurementGroup.TryGetValue("sensorGroup", out var sensorGroup))
                        {
                            if (key != "sensorGroup")
                            {
                                // Add a new column for each key in the MeasurementGroup
                                var column = new DataGridTextColumn
                                {
                                    Header = key, // Column header is the key from the measurement group
                                    Binding = new Binding($"SensorDataGroup[{key}]"), // Bind to the corresponding data in SensorData
                                    Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
                                };
                                RawDataTable.Columns.Add(column);
                            }
                        }
                    }
                }
            }

            UpdateData(pacifierItem);
        }

        private void UpdateData(PacifierItem pacifierItem)
        {
            // Populate the rows with data
            foreach (var sensorItem in pacifierItem.Sensors)
            {
                // Create a new SensorData entry for each SensorItem
                var sensorData = new SensorData
                {
                    Timestamp = sensorItem.dateTime.ToString("HH:mm:ss:ff"),
                    Sensor = sensorItem.SensorId
                };

                // Consolidate all keys and values from all MeasurementGroups into the SensorDataGroup
                foreach (var measurementGroup in sensorItem.MeasurementGroup)
                {
                    foreach (var keyValuePair in measurementGroup)
                    {
                        if (keyValuePair.Key == "sensorGroup") continue; // Skip "sensorGroup"
                        sensorData.SensorDataGroup[keyValuePair.Key] = keyValuePair.Value;
                        Debug.WriteLine($"Measurement is {keyValuePair.Key}: {keyValuePair.Value}");
                    }
                }

                // Add the populated sensorData to SensorEntries if it's a new entry
                SensorEntries.Add(sensorData);
            }
        }


        private void OnMeasurementGroupUpdated(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                UpdateData(PacifierItemT);
            }
        }
    }

    // Adjusted to hold dynamic sensor data as a dictionary
    public class SensorData
    {
        public string Timestamp { get; set; }
        public string Sensor { get; set; }
        public Dictionary<string, object> SensorDataGroup { get; set; } = new Dictionary<string, object>();
    }
}
