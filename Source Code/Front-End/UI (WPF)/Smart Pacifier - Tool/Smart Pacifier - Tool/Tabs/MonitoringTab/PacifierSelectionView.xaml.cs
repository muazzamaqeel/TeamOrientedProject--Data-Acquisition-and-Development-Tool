using Smart_Pacifier___Tool.Components;
using SmartPacifier.BackEnd.CommunicationLayer.Protobuf;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Linq;
using System;

namespace Smart_Pacifier___Tool.Tabs.MonitoringTab
{
    public partial class PacifierSelectionView : UserControl
    {
        private List<string> connectedPacifiers = new List<string>();
        private List<string> selectedPacifiers = new List<string>();

        public PacifierSelectionView()
        {
            InitializeComponent();

            // Subscribe to the SensorDataUpdated event
            ExposeSensorDataManager.Instance.SensorDataUpdated += OnSensorDataUpdated;

            // Load existing pacifier names
            LoadPacifierNames();
        }

        private void OnSensorDataUpdated(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LoadPacifierNames();
            });
        }

        private void LoadPacifierNames()
        {
            var pacifierIds = ExposeSensorDataManager.Instance.GetPacifierIds();

            // Debug: Print the count of pacifiers
            // MessageBox.Show($"Loaded {pacifierIds.Count} pacifiers.");

            foreach (var pacifierId in pacifierIds)
            {
                // Check if the pacifier is already in the list
                if (!connectedPacifiers.Contains(pacifierId))
                {
                    connectedPacifiers.Add(pacifierId);
                    AddConnectedPacifier(pacifierId);
                }
            }
        }

        public void AddConnectedPacifier(string pacifierId)
        {
            var connectedPacifierItem = new ConnectedPacifierItem
            {
                ButtonText = $"Pacifier {pacifierId}",
                IsChecked = false
            };

            connectedPacifierItem.Toggled += (s, e) =>
            {
                if (connectedPacifierItem.IsChecked)
                {
                    if (!selectedPacifiers.Contains(pacifierId))
                    {
                        selectedPacifiers.Add(pacifierId);
                        // Optionally, you can log or display a message
                        // MessageBox.Show($"{pacifierId} added to selected list.");
                    }
                }
                else
                {
                    selectedPacifiers.Remove(pacifierId);
                    // Optionally, you can log or display a message
                    // MessageBox.Show($"{pacifierId} removed from selected list.");
                }
            };

            Console.WriteLine($"Adding pacifier: {pacifierId}"); // Debug
            ConnectedPacifierPanel.Children.Add(connectedPacifierItem);
        }

        private void CreateCampaign_Click(object sender, RoutedEventArgs e)
        {
            string campaignName = CampaignTextBox.Text;

            // Validate that at least one pacifier is selected and the campaign name is not empty
            if (selectedPacifiers.Count > 0 && !string.IsNullOrWhiteSpace(campaignName))
            {
                var monitoringView = new MonitoringView();

                // Optionally pass selected pacifiers to the monitoring view
                // monitoringView.SetSelectedPacifiers(selectedPacifiers);

                if (this.Parent is ContentControl parent)
                {
                    parent.Content = monitoringView;
                }
            }
            else
            {
                MessageBox.Show("Please make sure there is at least one connected pacifier and the campaign name is not empty.");
            }
        }
    }
}
