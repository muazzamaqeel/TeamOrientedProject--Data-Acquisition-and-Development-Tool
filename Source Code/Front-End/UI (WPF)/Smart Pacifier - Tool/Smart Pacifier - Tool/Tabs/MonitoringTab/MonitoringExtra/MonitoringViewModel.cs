using Smart_Pacifier___Tool.Components;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Smart_Pacifier___Tool.Tabs.MonitoringTab.MonitoringExtra
{
    /// <summary>
    /// Separated ViewModel class to bind data
    /// </summary>
    public class MonitoringViewModel : INotifyPropertyChanged
    {

        private ObservableCollection<PacifierItem> _pacifierItems = [];
        private ObservableCollection<PacifierItem> _sensorItems = [];

        public List<PacifierItem> checkedPacifiers = [];
        public List<PacifierItem> checkedSensors = [];

        private readonly Dictionary<string, Dictionary<string, LineChartGraph>> _lineChartGraphs = [];

        // Create a list to hold the sensor types
        public HashSet<string> sensorTypes = [];

        public ObservableCollection<PacifierItem> PacifierItems
        {
            get => _pacifierItems;
            set
            {
                _pacifierItems = value;
                OnPropertyChanged(nameof(PacifierItems));
            }
        }

        public ObservableCollection<PacifierItem> SensorItems
        {
            get => _sensorItems;
            set
            {
                _sensorItems = value;
                OnPropertyChanged(nameof(SensorItems));
            }
        }

        public MonitoringViewModel()
        {
            PacifierItems = [];
            SensorItems = [];
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
