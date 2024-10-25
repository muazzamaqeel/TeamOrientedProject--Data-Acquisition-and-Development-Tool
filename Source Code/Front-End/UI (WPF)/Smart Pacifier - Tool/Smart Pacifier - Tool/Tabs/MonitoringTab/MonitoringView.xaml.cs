using Smart_Pacifier___Tool.Components;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Tabs.MonitoringTab
{
    public partial class MonitoringView : UserControl
    {
        public ObservableCollection<PacifierItem> PacifierItems { get; set; }
        public ObservableCollection<PacifierItem> SensorItems { get; set; }

        private int pacifierCounter = 1;
        private int sensorCounter = 1;

        private List<PacifierItem> checkedPacifiers = new List<PacifierItem>();
        private List<PacifierItem> checkedSensors = new List<PacifierItem>();

        public MonitoringView()
        {
            InitializeComponent();

            PacifierItems = new ObservableCollection<PacifierItem>();
            SensorItems = new ObservableCollection<PacifierItem>();

            // to use based on database
            AddPacifierItems(15); // temp
            AddSensorItems(15); // temp

            pacifierFilterPanel.ItemsSource = PacifierItems;
            sensorFilterPanel.ItemsSource = SensorItems;
        }

        private void AddPacifierItems(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var pacifierItem = new PacifierItem
                {
                    ButtonText = $"Pacifier {pacifierCounter}",
                    CircleText = " "
                };

                pacifierItem.ToggleChanged += (s, e) => UpdateCircleText(pacifierItem);
                pacifierCounter++;
                PacifierItems.Add(pacifierItem);
            }
        }

        private void AddSensorItems(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var sensorItem = new PacifierItem
                {
                    ButtonText = $"Sensor {sensorCounter}",
                    CircleText = " "
                };

                sensorItem.ToggleChanged += (s, e) => UpdateSensorCircleText(sensorItem);
                sensorCounter++;
                SensorItems.Add(sensorItem);
            }
        }

        private void UpdateCircleText(PacifierItem item)
        {
            if (item.IsChecked)
            {
                if (!checkedPacifiers.Contains(item))
                {
                    checkedPacifiers.Add(item);
                }
            }
            else
            {
                checkedPacifiers.Remove(item);
                item.CircleText = " ";
            }

            for (int i = 0; i < checkedPacifiers.Count; i++)
            {
                checkedPacifiers[i].CircleText = (i + 1).ToString();
            }
        }

        private void UpdateSensorCircleText(PacifierItem item)
        {
            if (item.IsChecked)
            {
                if (!checkedSensors.Contains(item))
                {
                    checkedSensors.Add(item);
                }
            }
            else
            {
                checkedSensors.Remove(item);
                item.CircleText = " ";
            }

            for (int i = 0; i < checkedSensors.Count; i++)
            {
                checkedSensors[i].CircleText = (i + 1).ToString();
            }
        }

        // for Vincent Raw Data View Testing
        private void OpenRawDataView_Click(object sender, RoutedEventArgs e)
        {
            // Define the properties to pass
            string pacifierName = "Pacifier 01";

            // Create an instance of RawDataView with the properties and a reference to this view
            var rawDataView = new RawDataView(pacifierName, this, true);

            // Replace the current view with RawDataView
            var parent = this.Parent as ContentControl;
            if (parent != null)
            {
                parent.Content = rawDataView;
            }
        }

    }

}
