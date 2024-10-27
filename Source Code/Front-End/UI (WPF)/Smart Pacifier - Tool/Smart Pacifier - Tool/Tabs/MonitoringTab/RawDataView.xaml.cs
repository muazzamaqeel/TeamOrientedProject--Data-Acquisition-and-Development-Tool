using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Tabs.MonitoringTab
{
    public partial class RawDataView : UserControl, INotifyPropertyChanged
    {
        private UserControl backLocation;
        private bool activeMonitoring;

        public string PacifierName { get; private set; }

        public bool ActiveMonitoring
        {
            get => activeMonitoring;
            set
            {
                if (activeMonitoring != value)
                {
                    activeMonitoring = value;
                    OnPropertyChanged(nameof(ActiveMonitoring));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Initializes a new instance of the Raw Data view, with a table.
        /// </summary>
        /// <param name="pacifierName">Name of the pacifier.</param>
        /// <param name="backLocation">The back location.</param>
        /// <param name="activeMonitoring">Set to true if active Monitoring otherwise to false</param>
        public RawDataView(string pacifierName, UserControl backLocation, bool activeMonitoring)
        {
            InitializeComponent();
            DataContext = this;
            PacifierName = pacifierName;
            this.backLocation = backLocation;
            ActiveMonitoring = !activeMonitoring;
            LoadData();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var parent = this.Parent as ContentControl;
            if (parent != null)
            {
                parent.Content = backLocation;
            }
        }

        public List<SensorData> SensorEntries { get; set; }

        // Dummy data
        private void LoadData()
        {
            SensorEntries = new List<SensorData>();
            var random = new Random();

            for (int i = 0; i < 1000; i++)
            {
                SensorEntries.Add(new SensorData
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-i).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Sensor1 = random.NextDouble() * 100,
                    Sensor2 = random.NextDouble() * 100,
                    Sensor3 = random.NextDouble() * 100,
                    Sensor4 = random.NextDouble() * 100,
                    Sensor5 = random.NextDouble() * 100
                });
            }
        }
    }

    public class SensorData
    {
        public string Timestamp { get; set; } = string.Empty; // Initialize with default value
        public double Sensor1 { get; set; }
        public double Sensor2 { get; set; }
        public double Sensor3 { get; set; }
        public double Sensor4 { get; set; }
        public double Sensor5 { get; set; }
    }
}