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
        public ObservableCollection<Sensor> Sensors 
        {
            get; 
            set; 
        }

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
            public ObservableCollection<SensorGroup> SensorGroups { get; set; }

            public Sensor(string sensorId)
            {
                SensorId = sensorId;
                SensorGroups = new ObservableCollection<SensorGroup>();
            }
        }

        public class SensorGroup
        {
            public string GroupName { get; set; }
            public MeasurementGroup MeasurementGroup { get; set; }

            public SensorGroup(string groupName)
            {
                GroupName = groupName;
                MeasurementGroup = new MeasurementGroup(groupName);
            }

            public void Add(string type)
            {
                // Implementation if needed in the future
            }
        }

        public class MeasurementGroup
        {
            public string GroupName { get; set; }
            public Dictionary<string, double> Measurements { get; set; }

            public MeasurementGroup(string groupName)
            {
                GroupName = groupName;
                Measurements = new Dictionary<string, double>();
            }

            public void AddOrUpdateMeasurement(string name, double value)
            {
                Measurements[name] = value;
            }

            public bool ContainsMeasurement(string name)
            {
                return Measurements.ContainsKey(name);
            }

            public double? GetMeasurement(string name)
            {
                return Measurements.TryGetValue(name, out var value) ? value : (double?)null;
            }
        }

    }
}
