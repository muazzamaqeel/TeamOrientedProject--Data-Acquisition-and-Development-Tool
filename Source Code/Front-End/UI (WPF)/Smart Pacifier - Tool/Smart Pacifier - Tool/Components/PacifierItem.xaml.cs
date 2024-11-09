using OxyPlot.Series;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Smart_Pacifier___Tool.Components
{
    public partial class PacifierItem : UserControl
    {
        public event EventHandler? ToggleChanged;

        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(PacifierItem), new PropertyMetadata("Pacifier"));

        public static readonly DependencyProperty CircleTextProperty =
            DependencyProperty.Register("CircleText", typeof(string), typeof(PacifierItem), new PropertyMetadata("1"));

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(PacifierItem), new PropertyMetadata(false));

        // New DependencyProperty for graph data
        public static readonly DependencyProperty GraphDataProperty =
            DependencyProperty.Register("GraphData", typeof(ObservableCollection<DataPoint>), typeof(PacifierItem), new PropertyMetadata(new ObservableCollection<DataPoint>()));

        public enum ItemType
        {
            Pacifier,
            Sensor
        }

        public ItemType Type
        {
            get;
            set;
        }

        // Only "Sensor" items have this graph
        public LineChartGraph LineChart
        {
            get;
            set;
        }

        public string PacifierId
        {
            get;
            set;
        }

        public string ItemId
        {
            get;
            set;
        }

        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }

        public string CircleText
        {
            get { return (string)GetValue(CircleTextProperty); }
            set { SetValue(CircleTextProperty, value); }
        }

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        // Property to hold graph data
        public ObservableCollection<DataPoint> GraphData
        {
            get { return (ObservableCollection<DataPoint>)GetValue(GraphDataProperty); }
            set { SetValue(GraphDataProperty, value); }
        }

        // Observable collection for sensor data
        public ObservableCollection<Sensor> Sensors { get; set; } = new ObservableCollection<Sensor>();

        private LineChartGraph? _graph;

        public PacifierItem(ItemType type)
        {
            InitializeComponent();

            DataContext = this;
            Type = type;
            this.IsChecked = false;

            Sensors = new ObservableCollection<Sensor>();
            PacifierId = "none";
            GraphData = new ObservableCollection<DataPoint>(); // Initialize the graph data collection
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                ToggleChanged?.Invoke(this, EventArgs.Empty);
                IsChecked = toggleButton.IsChecked == true; // Update the IsChecked property
            }
        }

        // DataPoint class to hold individual data points
        public class DataPoint
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        public class Sensor
        {
            public string SensorId { get; set; }
            public SensorGroup SensorGroup { get; set; }
            public MeasurementGroup MeasurementGroup { get; set; }

            public Sensor(string sensorId)
            {
                SensorId = sensorId;
                SensorGroup = new SensorGroup();
                MeasurementGroup= new MeasurementGroup();
            }
        }

        public class SensorGroup
        {
            public List<string> Types { get; set; }

            public SensorGroup()
            {
                Types = new List<string>();
            }

            public void Add(string type)
            {
                Types.Add(type);
            }
        }

        public class MeasurementGroup
        {
            public string GroupName { get; set; }
            public List<Measurement> Measurements { get; set; }

            // Default constructor
            public MeasurementGroup()
            {
                Measurements = new List<Measurement>();
            }

            // Constructor that accepts a group name
            public MeasurementGroup(string groupName) : this()  // Calls the default constructor
            {
                GroupName = groupName; // Set the GroupName from the parameter
            }

            // Method to add a measurement to the list, checks if the measurement exists
            public void AddMeasurement(string name, double value)
            {
                var existingMeasurement = Measurements.FirstOrDefault(m => m.Name == name);
                if (existingMeasurement != null)
                {
                    // Update the existing measurement
                    existingMeasurement.Value = value;
                }
                else
                {
                    // Add new measurement if it doesn't exist
                    Measurements.Add(new Measurement { Name = name, Value = value });
                }
            }

            // Optionally, you can add a method to retrieve a measurement by name if needed
            public Measurement GetMeasurement(string name)
            {
                return Measurements.FirstOrDefault(m => m.Name == name);
            }

            // Method to update an existing measurement
            public void UpdateMeasurement(string name, double newValue)
            {
                var existingMeasurement = Measurements.FirstOrDefault(m => m.Name == name);
                if (existingMeasurement != null)
                {
                    existingMeasurement.Value = newValue; // Update the measurement value
                }
            }

            // Method to check if a measurement exists
            public bool ContainsMeasurement(string name)
            {
                return Measurements.Any(m => m.Name == name);
            }
        }


        public class Measurement
        {
            public string Name { get; set; }
            public double Value { get; set; }
        }


    }
}
