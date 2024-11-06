using OxyPlot;
using OxyPlot.Series;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Components
{
    public partial class LineChartGraph : UserControl
    {
        private readonly LineSeries _series;

        public PlotModel PlotModel
        { 
            get; 
            private set; 
        }

        public string PlotId
        {
            get;
            set;
        }

        public int Interval
        {
            get;
            set;
        }

        public string SensorM
        {
            get;
            set;
        }

        public LineChartGraph()
        {
            InitializeComponent();

            SensorM = "sensor";
            PlotId = "none";
            Interval = 10;

            // Initialize the PlotModel and Series
            PlotModel = new PlotModel { Title = SensorM };
            _series = new LineSeries
            {
                Title = "NOT GOOD",
                MarkerType = MarkerType.Circle
            };
            
            PlotModel.Series.Add(_series);
            DataContext = this; // Set DataContext to enable data binding

        }

        public void UpdateData(double xValue, double yValue)
        {
            // Create a new DataPoint using the OxyPlot DataPoint constructor
            var dataPoint = new OxyPlot.DataPoint(xValue, yValue);

            // Add new data point to the series
            _series.Points.Add(dataPoint);

            // Limit the number of data points displayed, if needed
            if (_series.Points.Count > Interval) // adjust this value as required
            {
                _series.Points.RemoveAt(0);
            }

            // Refresh the plot to display the new data point
            PlotModel.InvalidatePlot(true);
        }

        public void UpdateDataPoints(ObservableCollection<DataPoint> graphData)
        {
            // Clear existing points
            _series.Points.Clear();

            // Add points from the graphData collection
            foreach (var point in graphData)
            {
                _series.Points.Add(new OxyPlot.DataPoint(point.X, point.Y));
            }

            // Refresh the plot to display the new data points
            PlotModel.InvalidatePlot(true);
        }

    }
}
