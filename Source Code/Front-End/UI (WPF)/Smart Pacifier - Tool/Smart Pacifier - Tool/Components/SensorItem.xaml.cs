using OxyPlot.Series;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using static Smart_Pacifier___Tool.Components.LineChartGraph;

namespace Smart_Pacifier___Tool.Components
{
    /// <summary>
    /// Interaction logic for SensorItem.xaml
    /// </summary>
    public partial class SensorItem : UserControl
    {

        public event EventHandler? ToggleChanged;

        public static readonly DependencyProperty SensorButtonTextProperty =
            DependencyProperty.Register("SensorButtonText", typeof(string), typeof(SensorItem), new PropertyMetadata("baseSensor"));

        public static readonly DependencyProperty SensorCircleTextProperty =
            DependencyProperty.Register("SensorCircleText", typeof(string), typeof(SensorItem), new PropertyMetadata("N/A"));

        public static readonly DependencyProperty SensorIsCheckedProperty =
            DependencyProperty.Register("SensorIsChecked", typeof(bool), typeof(SensorItem), new PropertyMetadata(false));

        public static readonly DependencyProperty SensorGroupsProperty =
            DependencyProperty.Register("SensorGroups", typeof(ObservableCollection<SensorGroup>), typeof(SensorItem), new PropertyMetadata(new ObservableCollection<SensorGroup>()));

        public static readonly DependencyProperty LinkedPacifiersProperty =
            DependencyProperty.Register("LinkedPacifiers", typeof(ObservableCollection<PacifierItem>), typeof(PacifierItem), new PropertyMetadata(new ObservableCollection<PacifierItem>()));

        // New DependencyProperty for graph data
        public static readonly DependencyProperty GraphDataProperty =
            DependencyProperty.Register("GraphData", typeof(ObservableCollection<DataPoint>), typeof(PacifierItem), new PropertyMetadata(new ObservableCollection<DataPoint>()));


        public string SensorId { get; set; }

        public bool HasGraphs { get; set; }

        public string SensorButtonText
        {
            get { return (string)GetValue(SensorButtonTextProperty); }
            set { SetValue(SensorButtonTextProperty, value); }
        }

        public string SensorCircleText
        {
            get { return (string)GetValue(SensorCircleTextProperty); }
            set { SetValue(SensorCircleTextProperty, value); }
        }

        public bool SensorIsChecked
        {
            get { return (bool)GetValue(SensorIsCheckedProperty); }
            set { SetValue(SensorIsCheckedProperty, value); }
        }

        public ObservableCollection<SensorGroup> SensorGroups
        {
            get { return (ObservableCollection<SensorGroup>)GetValue(SensorGroupsProperty); }
            set { SetValue(SensorGroupsProperty, value); }
        }

        public ObservableCollection<PacifierItem> LinkedPacifiers 
        {
            get { return (ObservableCollection<PacifierItem>)GetValue(LinkedPacifiersProperty); }
            set { SetValue(LinkedPacifiersProperty, value); }
        } 
            


        // Reference to the parent PacifierItem
        public PacifierItem ParentPacifierItem { get; set; } // Parent

        public SensorItem(string sensorId, PacifierItem parentPacifierItem)
        {
            InitializeComponent();
            SensorId = sensorId;
            SensorGroups = new ObservableCollection<SensorGroup>();
            LinkedPacifiers = new ObservableCollection<PacifierItem>(); // Track which pacifiers use this sensor
            DataContext = this;
            HasGraphs = false;

            ParentPacifierItem = parentPacifierItem;

        }

        public SensorItem GetSensorItem()
        {
            return this;
        }

        private void SensorToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                ToggleChanged?.Invoke(this, EventArgs.Empty);
                SensorIsChecked = toggleButton.IsChecked == true; // Update the IsChecked property
            }
        }
    }

    public class SensorGroup
    {
        public string GroupName { get; set; }
        public MeasurementGroup MeasurementGroup { get; set; }

        // Reference to the parent SensorItem
        public SensorItem ParentSensorItem { get; set; }

        public SensorGroup(string groupName, SensorItem parentSensorItem)
        {
            GroupName = groupName;

            MeasurementGroup = new MeasurementGroup(groupName, this);

            ParentSensorItem = parentSensorItem;
        }

        public SensorGroup GetSensorGroup()
        {
            return this;
        }
    }

    public class MeasurementGroup(string groupName, SensorGroup parentSensorGroup)
    {
        public string? SensorGroup { get; set; }
        public string GroupName { get; set; } = groupName;
        public Dictionary<string, double> Measurements { get; set; } = new Dictionary<string, double>();

        // Reference to the parent SensorGroup
        public SensorGroup ParentSensorGroup { get; set; } = parentSensorGroup;

        public MeasurementGroup GetMeasurementGroup()
        {
            return this;
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
