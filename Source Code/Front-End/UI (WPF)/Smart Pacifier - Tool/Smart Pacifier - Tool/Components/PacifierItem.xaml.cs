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

        private LineChartGraph? _graph;

        //public MonitoringSensorData SensorData { get; set; }

        public PacifierItem(ItemType type)
        {
            InitializeComponent();
            DataContext = this;
            Type = type;
            if (Type == ItemType.Sensor)
            {
                LineChart = new LineChartGraph();
            }

            PacifierId = "none";
            GraphData = []; // Initialize the graph data collection

            // Subscribe to GraphData changes
            GraphData.CollectionChanged += OnGraphDataChanged;

            // Initialize and bind the graph if toggle is on
            ToggleChanged += (s, e) => InitializeGraphIfNeeded();

        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                ToggleChanged?.Invoke(this, EventArgs.Empty);
                IsChecked = toggleButton.IsChecked == true; // Update the IsChecked property
            }
        }

        private void OnGraphDataChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Update the LineChartGraph with the new data points
            if (LineChart != null)
            {
                LineChart.UpdateDataPoints(GraphData);
            }
        }

        private void InitializeGraphIfNeeded()
        {
            if (_graph == null && IsChecked)
            {
                _graph = new LineChartGraph();

                // Bind the graph’s series to GraphData
                var series = new LineSeries();
                foreach (var point in GraphData)
                {
                    series.Points.Add(new OxyPlot.DataPoint(point.X, point.Y));
                }

                _graph.PlotModel.Series.Add(series);
            }
        }
    }

    // DataPoint class to hold individual data points
    public class DataPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
