using OxyPlot;
using OxyPlot.Series;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using static Smart_Pacifier___Tool.Components.PacifierItem;

namespace Smart_Pacifier___Tool.Components
{
    public partial class LineChartGraph : UserControl
    {
        private readonly LineSeries _series;

        // PlotModel represents the plot data for OxyPlot
        public PlotModel PlotModel { get; private set; }

        // ID of the plot (can be used for identifying specific plots)
        public string PlotId { get; set; }

        // The number of data points to display before removing the oldest
        public double Interval { get; set; }

        // The label for the measurement group
        public string GroupName { get; set; }

        // Type of the sensor (e.g., temperature, humidity)
        public string? SensorId { get; set; }

        // DependencyProperty to bind an ObservableCollection of DataPoints
        public static readonly DependencyProperty DataPointsProperty =
            DependencyProperty.Register(
                nameof(DataPoints),
                typeof(ObservableCollection<DataPoint>),
                typeof(LineChartGraph),
                new PropertyMetadata(new ObservableCollection<DataPoint>(), OnDataPointsChanged));

        // ObservableCollection for binding DataPoints to the graph
        public ObservableCollection<DataPoint> DataPoints
        {
            get => (ObservableCollection<DataPoint>)GetValue(DataPointsProperty);
            set => SetValue(DataPointsProperty, value);
        }

        // Constructor to initialize the LineChartGraph
        public LineChartGraph(SensorGroup sensorGroup, string plotId, double interval)
        {
            InitializeComponent();

            // Default values
            GroupName = sensorGroup.GroupName;
            PlotId = plotId;
            Interval = interval;

            // Initialize the PlotModel and LineSeries
            PlotModel = new PlotModel { Title = GroupName };
            _series = new LineSeries
            {
                Title = SensorId,
                MarkerType = MarkerType.Circle
            };

            // Add the series to the PlotModel
            PlotModel.Series.Add(_series);
            DataContext = this; // Enable data binding
        }

        // Method to update the plot with new data points when DataPoints collection changes
        private static void OnDataPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as LineChartGraph;
            if (chart != null)
            {
                chart.UpdateDataPoints(chart.DataPoints); // Ensure this properly updates the graph
            }
        }


        // Update the graph with a new data point
        public void UpdateData(double xValue, double yValue)
        {

            Debug.WriteLine($"Updating data point: {xValue}, {yValue}");

            // Update ObservableCollection directly
            DataPoints.Add(new DataPoint(xValue, yValue));

            // Limit number of data points to 'Interval'
            if (DataPoints.Count > Interval)
            {
                DataPoints.RemoveAt(0);
            }

            // Refresh the plot
            PlotModel.InvalidatePlot(true);
        }


        // Update the graph with a collection of data points
        public void UpdateDataPoints(ObservableCollection<DataPoint> graphData)
        {
            // Clear the existing points and add the new ones
            _series.Points.Clear();
            foreach (var point in graphData)
            {
                _series.Points.Add(new OxyPlot.DataPoint(point.X, point.Y));
            }

            // Refresh the plot
            PlotModel.InvalidatePlot(true);
        }

        // Simple class to represent a DataPoint with X and Y values
        public class DataPoint
        {
            public double X { get; set; }
            public double Y { get; set; }

            public DataPoint(double x, double y)
            {
                X = x;
                Y = y;
            }
        }
    }
}
