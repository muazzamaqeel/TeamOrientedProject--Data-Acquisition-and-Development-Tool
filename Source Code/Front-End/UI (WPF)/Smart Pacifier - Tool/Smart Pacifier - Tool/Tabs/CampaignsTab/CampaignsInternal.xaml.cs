using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Smart_Pacifier___Tool.Components;
using Smart_Pacifier___Tool.Tabs.MonitoringTab.MonitoringExtra;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.CampaignsTab
{
    /// <summary>
    /// Interaction logic for CampaignsInternal.xaml
    /// </summary>
    public partial class CampaignsInternal : UserControl
    {
        private readonly IManagerCampaign? _managerCampaign;
        private readonly string? _campaignName;
        private readonly MonitoringViewModel? _viewModel;

        // Collection to hold pacifiers for display
        public ObservableCollection<PacifierItem> Pacifiers { get; private set; } = new ObservableCollection<PacifierItem>();

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
        }

        // Load pacifiers associated with the campaign
        private async void LoadPacifiersForCampaign()
        {
            var pacifierNames = await _managerCampaign.GetPacifiersByCampaignNameAsync(_campaignName);

            var uniquePacifiers = new HashSet<string>(pacifierNames);

            foreach (var pacifierName in uniquePacifiers)
            {
                // Create a PacifierItem with the actual name
                var pacifierItem = new PacifierItem(pacifierName)
                {
                    ButtonText = pacifierName // Set ButtonText to the pacifier name
                };
                pacifierItem.ToggleChanged += PacifierItem_Toggled;
                Pacifiers.Add(pacifierItem);
            }

            campaignFilterPanel.ItemsSource = Pacifiers;
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

                    var uniqueSensors = new HashSet<string>(sensors); // Ensure unique sensors
                    DisplaySensors(uniqueSensors, pacifierItem);
                }
                else
                {
                    // Remove sensors from the view when pacifier is unchecked
                    RemoveSensors(pacifierItem);
                }
            }
        }

        // Display sensors for a selected pacifier
        private void DisplaySensors(IEnumerable<string> sensors, PacifierItem pacifierItem)
        {
            foreach (var sensorName in sensors)
            {
                var sensorItem = new SensorItem(sensorName, pacifierItem)
                {
                    SensorButtonText = sensorName // Set ButtonText to the sensor name
                };
                dataFilterPanel.Items.Add(sensorItem);
            }
        }

        // Remove sensors for the unselected pacifier
        private void RemoveSensors(PacifierItem pacifierItem)
        {
            var sensorsToRemove = dataFilterPanel.Items
                .OfType<SensorItem>()
                .Where(sensor => sensor.ParentPacifierItem == pacifierItem)
                .ToList();

            foreach (var sensor in sensorsToRemove)
            {
                dataFilterPanel.Items.Remove(sensor);
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
