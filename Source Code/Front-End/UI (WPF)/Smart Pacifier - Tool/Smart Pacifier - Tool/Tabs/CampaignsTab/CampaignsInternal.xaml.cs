using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Smart_Pacifier___Tool.Components;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.CampaignsTab
{
    public partial class CampaignsInternal : UserControl
    {
        private readonly IManagerCampaign? _managerCampaign;
        private readonly string? _campaignName;

        // Collection to hold pacifiers and sensors for display
        public ObservableCollection<PacifierItem> Pacifiers { get; private set; } = new ObservableCollection<PacifierItem>();
        private ObservableCollection<SensorItem> SelectedSensors = new ObservableCollection<SensorItem>();

        // Constructor accepting the database manager and campaign name
        public CampaignsInternal(IManagerCampaign managerCampaign, string campaignName)
        {
            InitializeComponent();
            _managerCampaign = managerCampaign;
            _campaignName = campaignName;

            // Set the campaign title
            CampaignTitle.Text = $"{_campaignName}";

            // Load the pacifiers for this campaign
            LoadPacifiersForCampaign();

            // Set ItemsSource for the panels
            campaignFilterPanel.ItemsSource = Pacifiers;
            dataFilterPanel.ItemsSource = SelectedSensors;
        }

        // Load pacifiers associated with the campaign
        private async void LoadPacifiersForCampaign()
        {
            var pacifierNames = await _managerCampaign.GetPacifiersByCampaignNameAsync(_campaignName);

            foreach (var pacifierName in new HashSet<string>(pacifierNames))
            {
                var pacifierItem = new PacifierItem(pacifierName)
                {
                    ButtonText = pacifierName,
                    CircleText = " ",
                };
                pacifierItem.ToggleChanged += PacifierItem_Toggled;
                Pacifiers.Add(pacifierItem);
            }
        }

        // Handle pacifier toggle to show associated sensors
        private async void PacifierItem_Toggled(object? sender, EventArgs e)
        {
            if (sender is PacifierItem pacifierItem)
            {
                if (pacifierItem.IsChecked)
                {
                    // Load and display unique sensors for the selected pacifier
                    var sensors = await _managerCampaign.GetSensorsByPacifierNameAsync(pacifierItem.PacifierId, _campaignName);

                    DisplaySensors(sensors.Distinct(), pacifierItem);
                }
                else
                {
                    // Remove sensors from the view when pacifier is unchecked
                    RemoveSensors(pacifierItem);
                }
                UpdateCircleText(); // Update pacifier circle text order
            }
        }

        // Display sensors for a selected pacifier
        private void DisplaySensors(IEnumerable<string> sensors, PacifierItem pacifierItem)
        {
            foreach (var sensorName in sensors)
            {
                var sensorItem = new SensorItem(sensorName, pacifierItem)
                {
                    SensorButtonText = sensorName,
                    SensorCircleText = " "
                };
                sensorItem.ToggleChanged += (s, e) => UpdateSensorCircleText();
                SelectedSensors.Add(sensorItem);
            }
            UpdateSensorCircleText(); // Immediately update circle text to ensure correct order
        }

        // Remove sensors for the unselected pacifier
        private void RemoveSensors(PacifierItem pacifierItem)
        {
            var sensorsToRemove = SelectedSensors.Where(sensor => sensor.ParentPacifierItem == pacifierItem).ToList();

            foreach (var sensor in sensorsToRemove)
            {
                SelectedSensors.Remove(sensor);
            }
            UpdateSensorCircleText(); // Refresh circle text for remaining sensors
        }

        // Update circle text based on the order of selected pacifiers
        private void UpdateCircleText()
        {
            int order = 1;
            foreach (var pacifier in Pacifiers.Where(p => p.IsChecked))
            {
                pacifier.CircleText = order.ToString();
                order++;
            }
            foreach (var pacifier in Pacifiers.Where(p => !p.IsChecked))
            {
                pacifier.CircleText = " ";
            }
        }

        // Update circle text based on the order of selected sensors
        private void UpdateSensorCircleText()
        {
            int order = 1;
            foreach (var sensor in SelectedSensors.Where(s => s.SensorIsChecked))
            {
                sensor.SensorCircleText = order.ToString();
                order++;
            }
            foreach (var sensor in SelectedSensors.Where(s => !s.SensorIsChecked))
            {
                sensor.SensorCircleText = " ";
            }
        }

        // Placeholder event handler for New Campaign button
        private void NewCampaign_Button(object sender, RoutedEventArgs e)
        {
            // Empty logic for New Campaign functionality
        }

        // Placeholder event handler for Delete Campaign button
        private void DeleteCampaign_Button(object sender, RoutedEventArgs e)
        {
            // Empty logic for Delete Campaign functionality
        }

        // Placeholder event handler for Export Data button
        private void ExportData_Button(object sender, RoutedEventArgs e)
        {
            // Empty logic for Export Data functionality
        }
    }
}
