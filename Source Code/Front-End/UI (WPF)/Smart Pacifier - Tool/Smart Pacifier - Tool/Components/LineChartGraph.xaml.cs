using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DataPoint = OxyPlot.DataPoint;

namespace Smart_Pacifier___Tool.Components
{
    /// <summary>
    /// Interaction logic for LineChartGraph.xaml
    /// </summary>
    public partial class LineChartGraph : UserControl
    {
        /// <summary>
        /// Gets the PlotModel representing the plot data for OxyPlot.
        /// </summary>
        public PlotModel PlotModel { get; private set; }

        /// <summary>
        /// ObservableCollection of LineSeries for the plot.
        /// </summary>
        public ObservableCollection<LineSeries> LineSeriesCollection { get; private set; }

        /// <summary>
        /// Gets or sets the ID of the plot (can be used for identifying specific plots).
        /// </summary>
        public string PlotId { get; set; }

        /// <summary>
        /// Gets or sets the number of data points to display before removing the oldest.
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Gets or sets the label for the measurement group.
        /// </summary>
        public string GroupName { get; set; }

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
        /// <param name="sensorItem"></param>
        /// <param name="interval"></param>
        /// <param name="groupName"></param>
        public LineChartGraph(SensorItem sensorItem, string groupName, int interval)
        {
            InitializeComponent();

            // Default values
            Interval = interval;
            GroupName = groupName;

            OxyColor foregroundColor = ConvertBrushToOxyColor((SolidColorBrush)Application.Current.FindResource("MainViewForegroundColor"));

            PlotModel = new PlotModel
            {
                Title = GroupName,
                TextColor = foregroundColor,
                TitleColor = foregroundColor,
                TitleHorizontalAlignment = TitleHorizontalAlignment.CenteredWithinView

            };

            PlotModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm:ss",
                FontSize = 10,
                Title = "Time",
                IsZoomEnabled = false,
                IsPanEnabled = false,
                IntervalLength = 50,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                TextColor = foregroundColor,
                TitleColor = foregroundColor
            });

            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                TextColor = foregroundColor,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                TitleColor = foregroundColor
            });

            // Add a legend to the plot model
            PlotModel.Legends.Add(new Legend
            {
                LegendPosition = LegendPosition.TopRight,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendBorderThickness = 0,
                TextColor = foregroundColor
            });



            LineSeriesCollection = new ObservableCollection<LineSeries>();

            DataContext = this; // Enable data binding
        }
        private OxyColor ConvertBrushToOxyColor(SolidColorBrush brush)
        {
            Color color = brush.Color;
            return OxyColor.FromArgb(color.A, color.R, color.G, color.B);
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
        /// Updates the graph with a new data point for a specific series.
        /// </summary>
        /// <param name="seriesIndex">Index of the series to update.</param>
        /// <param name="xValue">The X value of the data point.</param>
        /// <param name="yValue">The Y value of the data point.</param>
        public void UpdateData(int seriesIndex, double xValue, double yValue)
        {
            if (seriesIndex >= 0 && seriesIndex < LineSeriesCollection.Count)
            {
                var series = LineSeriesCollection[seriesIndex];
                series.Points.Add(new DataPoint(xValue, yValue));

                // Limit number of data points to 'Interval'
                if (series.Points.Count > Interval)
                {
                    series.Points.RemoveAt(0);
                }

                // Refresh the plot
                PlotModel.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// Adds a new LineSeries to the plot.
        /// </summary>
        /// <param name="sensorId">Sensor identifier for the new series.</param>
        /// <param name="interval">The interval for the data points.</param>
        public void AddLineSeries(string measurementGroup)
        {
            var newSeries = new LineSeries
            {
                Title = measurementGroup,
                MarkerType = MarkerType.None
            };

            LineSeriesCollection.Add(newSeries);
            PlotModel.Series.Add(newSeries);

            // Refresh the plot
            PlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Removes a LineSeries from the plot by index.
        /// </summary>
        /// <param name="seriesIndex">Index of the series to remove.</param>
        public void RemoveLineSeries(int seriesIndex)
        {
            if (seriesIndex >= 0 && seriesIndex < LineSeriesCollection.Count)
            {
                var series = LineSeriesCollection[seriesIndex];
                LineSeriesCollection.RemoveAt(seriesIndex);
                PlotModel.Series.Remove(series);

                // Refresh the plot
                PlotModel.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// Updates the graph with a collection of data points.
        /// </summary>
        /// <param name="graphData">The collection of data points.</param>
        public void UpdateDataPoints(ObservableCollection<DataPoint> graphData)
        {
            // Clear the existing points and add the new ones
            foreach (var series in LineSeriesCollection)
            {
                series.Points.Clear();
            }

            foreach (var point in graphData)
            {
                // Add the points to all series, or customize per series
                foreach (var series in LineSeriesCollection)
                {
                    series.Points.Add(new OxyPlot.DataPoint(point.X, point.Y));
                }
            }

            // Refresh the plot
            PlotModel.InvalidatePlot(true);
        }
    }
}
