using OxyPlot;
using OxyPlot.Series;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Components
{
    public partial class LineChartGraph : UserControl
    {
        public PlotModel PlotModel { get; private set; }

        public LineChartGraph()
        {
            InitializeComponent();
            PlotModel = new PlotModel { Title = "Line Chart Example" };

            // Sample data series
            var series = new LineSeries
            {
                Title = "Sample Data",
                MarkerType = MarkerType.Circle
            };

            // Add some points to the series
            series.Points.Add(new DataPoint(0, 0));
            series.Points.Add(new DataPoint(1, 2));
            series.Points.Add(new DataPoint(2, 1));
            series.Points.Add(new DataPoint(3, 4));
            series.Points.Add(new DataPoint(4, 3));

            PlotModel.Series.Add(series);
            DataContext = this; // Set DataContext to enable data binding
        }
    }
}
