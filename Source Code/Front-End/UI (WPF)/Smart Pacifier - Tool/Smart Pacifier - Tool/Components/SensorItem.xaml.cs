using OxyPlot.Series;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;
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

        public static readonly DependencyProperty LinkedPacifiersProperty =
            DependencyProperty.Register("LinkedPacifiers", typeof(ObservableCollection<PacifierItem>), typeof(SensorItem), new PropertyMetadata(new ObservableCollection<PacifierItem>()));

        public static readonly DependencyProperty SensorGraphsProperty =
            DependencyProperty.Register("SensorGraphs", typeof(ObservableCollection<LineChartGraph>), typeof(SensorItem), new PropertyMetadata(new ObservableCollection<LineChartGraph>()));

        public static readonly DependencyProperty SensorGroupsProperty =
            DependencyProperty.Register("SensorGroups", typeof(ObservableCollection<string>), typeof(SensorItem), new PropertyMetadata(new ObservableCollection<string>()));

        public static readonly DependencyProperty MeasurementGroupProperty =
            DependencyProperty.Register("MeasurementGroup", typeof(ObservableCollection<Dictionary<string, object>>), typeof(SensorItem), new PropertyMetadata(new ObservableCollection<Dictionary<string, object>>()));


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

        public ObservableCollection<PacifierItem> LinkedPacifiers 
        {
            get { return (ObservableCollection<PacifierItem>)GetValue(LinkedPacifiersProperty); }
            set { SetValue(LinkedPacifiersProperty, value); }
        }

        public ObservableCollection<string> SensorGroups
        {
            get { return (ObservableCollection<string>)GetValue(SensorGroupsProperty); }
            set { SetValue(SensorGroupsProperty, value); }
        }

        public ObservableCollection<Dictionary<string, object>> MeasurementGroup
        {
            get { return (ObservableCollection<Dictionary<string, object>>)GetValue(MeasurementGroupProperty); }
            set { SetValue(MeasurementGroupProperty, value); }
        }

        public ObservableCollection<Dictionary<string, object>> EventMeasurementGroup
        {
            get { return (ObservableCollection<Dictionary<string, object>>)GetValue(MeasurementGroupProperty); }
            set { SetValue(MeasurementGroupProperty, value); }
        }

        public ObservableCollection<LineChartGraph> SensorGraphs
        {
            get { return (ObservableCollection<LineChartGraph>)GetValue(SensorGraphsProperty); }
            set { SetValue(SensorGraphsProperty, value); }
        }

        public DateTime dateTime { get; set; }

        // Reference to the parent PacifierItem
        public PacifierItem ParentPacifierItem { get; set; } // Parent

        public SensorItem(string sensorId, PacifierItem parentPacifierItem)
        {
            InitializeComponent();
            SensorId = sensorId;
            MeasurementGroup = new ObservableCollection<Dictionary<string, object>>();
            SensorGroups = new ObservableCollection<string>();
            LinkedPacifiers = new ObservableCollection<PacifierItem>(); // Track which pacifiers use this sensor
            SensorGraphs = new ObservableCollection<LineChartGraph>();
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

}
