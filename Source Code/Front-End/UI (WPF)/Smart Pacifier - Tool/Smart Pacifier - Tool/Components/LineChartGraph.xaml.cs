using OxyPlot;
using OxyPlot.Series;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using static Smart_Pacifier___Tool.Components.PacifierItem;

namespace Smart_Pacifier___Tool.Components
{
    /// <summary>
    /// Interaction logic for LineChartGraph.xaml
    /// </summary>
    public partial class LineChartGraph : UserControl
    {
        private readonly LineSeries _series;

        /// <summary>
        /// Gets the PlotModel representing the plot data for OxyPlot.
        /// </summary>
        public PlotModel PlotModel { get; private set; }

        /// <summary>
        /// Gets or sets the ID of the plot (can be used for identifying specific plots).
        /// </summary>
        public string PlotId { get; set; }

        /// <summary>
        /// Gets or sets the number of data points to display before removing the oldest.
        /// </summary>
        public double Interval { get; set; }

        /// <summary>
        /// Gets or sets the label for the measurement group.
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// Gets or sets the type of the sensor (e.g., temperature, humidity).
        /// </summary>
        public string? SensorId { get; set; }

        /// <summary>
        /// DependencyProperty to bind an ObservableCollection of DataPoints.
        /// </summary>
        public static readonly DependencyProperty DataPointsProperty =
            DependencyProperty.Register(
                nameof(DataPoints),
                typeof(ObservableCollection<DataPoint>),
                typeof(LineChartGraph),
                new PropertyMetadata(new ObservableCollection<DataPoint>(), OnDataPointsChanged));

        /// <summary>
        /// Gets or sets the ObservableCollection for binding DataPoints to the graph.
        /// </summary>
        public ObservableCollection<DataPoint> DataPoints
        {
            get => (ObservableCollection<DataPoint>)GetValue(DataPointsProperty);
            set => SetValue(DataPointsProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineChartGraph"/> class.
        /// </summary>
        /// <param name="sensorGroup">The sensor group.</param>
        /// <param name="plotId">The plot ID.</param>
        /// <param name="interval">The interval for data points.</param>
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

        /// <summary>
        /// Method to update the plot with new data points when DataPoints collection changes.
        /// </summary>
        private static void OnDataPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as LineChartGraph;
            if (chart != null)
            {
                chart.UpdateDataPoints(chart.DataPoints); // Ensure this properly updates the graph
            }
        }

        /// <summary>
        /// Updates the graph with a new data point.
        /// </summary>
        /// <param name="xValue">The X value of the data point.</param>
        /// <param name="yValue">The Y value of the data point.</param>
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

        /// <summary>
        /// Updates the graph with a collection of data points.
        /// </summary>
        /// <param name="graphData">The collection of data points.</param>
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

        /// <summary>
        /// Simple class to represent a DataPoint with X and Y values.
        /// </summary>
        public class DataPoint
        {
            /// <summary>
            /// Gets or sets the X value of the data point.
            /// </summary>
            public double X { get; set; }

            /// <summary>
            /// Gets or sets the Y value of the data point.
            /// </summary>
            public double Y { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="DataPoint"/> class.
            /// </summary>
            /// <param name="x">The X value.</param>
            /// <param name="y">The Y value.</param>
            public DataPoint(double x, double y)
            {
                X = x;
                Y = y;
            }
        }
    }
}