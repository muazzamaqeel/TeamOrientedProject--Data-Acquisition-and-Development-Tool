using Smart_Pacifier___Tool.Components;
using SmartPacifier.BackEnd.CommunicationLayer.MQTT;
using SmartPacifier.BackEnd.CommunicationLayer.Protobuf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using static Smart_Pacifier___Tool.Components.LineChartGraph;
using static SmartPacifier.BackEnd.CommunicationLayer.MQTT.Broker;
using System.Windows.Controls;
using static Smart_Pacifier___Tool.Components.PacifierItem;
using System.Runtime.Intrinsics.X86;
using System.IO.Packaging;

namespace Smart_Pacifier___Tool.Tabs.CampaignsTab
{
    public class CampaignViewModel : INotifyPropertyChanged
    {
        // ObservableCollections to bind to UI for Pacifiers and Sensors
        public ObservableCollection<PacifierItem> _pacifierItems = new ObservableCollection<PacifierItem>();
        public ObservableCollection<SensorItem> _sensorItems = new ObservableCollection<SensorItem>();

        // Memorizes the order for toggling buttons
        public ObservableCollection<PacifierItem> _checkedPacifierItems = new ObservableCollection<PacifierItem>();
        public ObservableCollection<SensorItem> _checkedSensorItems = new ObservableCollection<SensorItem>();

        // Maps for storing grid and row references
        public Dictionary<PacifierItem, Grid> PacifierGridMap = new Dictionary<PacifierItem, Grid>();
        public Dictionary<Tuple<PacifierItem, SensorItem>, RowDefinition> SensorRowMap = new Dictionary<Tuple<PacifierItem, SensorItem>, RowDefinition>();

        // Dictionary mapping PacifierItem to its associated sensors
        public Dictionary<PacifierItem, ObservableCollection<SensorItem>> PacifierToSensorsMap = new Dictionary<PacifierItem, ObservableCollection<SensorItem>>();
        // Dictionary mapping SensorItem to its associated pacifiers
        public Dictionary<SensorItem, ObservableCollection<PacifierItem>> SensorToPacifiersMap = new Dictionary<SensorItem, ObservableCollection<PacifierItem>>();

        // Dictionary to hold LineChartGraph objects per sensor ID
        public Dictionary<string, Dictionary<string, LineChartGraph>> _lineChartGraphs = new Dictionary<string, Dictionary<string, LineChartGraph>>();

        // ObservableCollections to hold sensor types and plot types
        private ObservableCollection<string> _sensorTypes = new ObservableCollection<string>();
        private ObservableCollection<string> _plotTypes = new ObservableCollection<string>();
        private ObservableCollection<string> _sensorMeasurements = new ObservableCollection<string>();
        private ObservableCollection<string> _groupedSensorMeasurements = new ObservableCollection<string>();
        private ObservableCollection<string> _sensorValues = new ObservableCollection<string>();

        private Dictionary<string, ObservableCollection<string>> _groupedMeasurements = new Dictionary<string, ObservableCollection<string>>();

        private string _campaignName;

        // Dictionary to store real-time sensor data (as ObservableDictionary for binding)
        public Dictionary<string, SensorItem> _sensorDataDictionary = new Dictionary<string, SensorItem>();

        // Properties to expose ObservableCollections for Pacifier and Sensor items
        public ObservableCollection<PacifierItem> PacifierItems
        {
            get => _pacifierItems;
            set
            {
                _pacifierItems = value;
                OnPropertyChanged(nameof(PacifierItems));
            }
        }

        public ObservableCollection<SensorItem> SensorItems
        {
            get => _sensorItems;
            set
            {
                _sensorItems = value;
                OnPropertyChanged(nameof(SensorItems));
            }
        }

        public ObservableCollection<PacifierItem> CheckedPacifierItems
        {
            get => _checkedPacifierItems;
            set
            {
                _checkedPacifierItems = value;
                OnPropertyChanged(nameof(PacifierItems));
            }
        }

        public ObservableCollection<SensorItem> CheckedSensorItems
        {
            get => _checkedSensorItems;
            set
            {
                _checkedSensorItems = value;
                OnPropertyChanged(nameof(CheckedSensorItems));
            }
        }

        // Properties to bind sensorTypes and plotTypes to the UI
        public ObservableCollection<string> SensorTypes
        {
            get => _sensorTypes;
            set
            {
                _sensorTypes = value;
                OnPropertyChanged(nameof(SensorTypes));
            }
        }

        public ObservableCollection<string> PlotTypes
        {
            get => _plotTypes;
            set
            {
                _plotTypes = value;
                OnPropertyChanged(nameof(PlotTypes));
            }
        }

        // Properties to bind sensorMeasurements and plotTypes to the UI
        public ObservableCollection<string> SensorMeasurements
        {
            get => _sensorMeasurements;
            set
            {
                _sensorMeasurements = value;
                OnPropertyChanged(nameof(SensorMeasurements));
            }
        }

        // Properties to bind GroupedSensorMeasurements and plotTypes to the UI
        public ObservableCollection<string> GroupedSensorMeasurements
        {
            get => _groupedSensorMeasurements;
            set
            {
                _sensorMeasurements = value;
                OnPropertyChanged(nameof(GroupedSensorMeasurements));
            }
        }

        // Properties to bind GroupedSensorMeasurements and plotTypes to the UI
        public ObservableCollection<string> SensorValues
        {
            get => _sensorValues;
            set
            {
                _sensorValues = value;
                OnPropertyChanged(nameof(SensorValues));
            }
        }

        // Property for CampaignName
        public string CampaignName
        {
            get => _campaignName;
            set
            {
                if (_campaignName != value)
                {
                    _campaignName = value;
                    OnPropertyChanged(nameof(CampaignName));
                }
            }
        }

        // Constructor initializing empty ObservableCollections
        public CampaignViewModel()
        {
            PacifierItems = new ObservableCollection<PacifierItem>();
            SensorItems = new ObservableCollection<SensorItem>();
            SensorTypes = new ObservableCollection<string>();
            PlotTypes = new ObservableCollection<string>();
            SensorMeasurements = new ObservableCollection<string>();
            GroupedSensorMeasurements = new ObservableCollection<string>();
            _sensorDataDictionary = new Dictionary<string, SensorItem>();

        }

        public void TogglePacifierVisibility(PacifierItem pacifierItem)
        {
            bool toggledPacifiers = PacifierItems.Any(p => p.IsChecked);
            // Make all SensorItems visible if pacifier item is toggled on
            foreach (var sensorItem in SensorItems)
            {
                sensorItem.Visibility = toggledPacifiers ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // Property changed notification
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class SensorDetailsWindow : Window
    {
        public SensorDetailsWindow(string details)
        {
            Title = "Sensor Details";
            Width = 400;
            Height = 300;

            var textBox = new TextBox
            {
                Text = details,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var grid = new Grid();
            grid.Children.Add(textBox);
            Content = grid;
        }
    }

}
