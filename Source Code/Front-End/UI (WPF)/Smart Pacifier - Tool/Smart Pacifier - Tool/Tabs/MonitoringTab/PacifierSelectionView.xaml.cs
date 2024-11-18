using Smart_Pacifier___Tool.Components;
using SmartPacifier.BackEnd.CommunicationLayer.Protobuf;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.ObjectModel;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.LineProtocol;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.MonitoringTab
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PacifierSelectionView : UserControl
    {
        private ObservableCollection<PacifierItem> connectedPacifiers = [];
        private ObservableCollection<PacifierItem> selectedPacifiers = [];

        /// <summary>
        /// 
        /// </summary>
        public PacifierSelectionView()
        {
            InitializeComponent();

            // Subscribe to the SensorDataUpdated event
            ExposeSensorDataManager.Instance.SensorDataUpdated += OnSensorDataUpdated;

            // Load existing pacifier names
            LoadPacifierNames();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSensorDataUpdated(object? sender, EventArgs? e)
        {
            Dispatcher.Invoke(() =>
            {
                LoadPacifierNames();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadPacifierNames()
        {
            var pacifierIds = ExposeSensorDataManager.Instance.GetPacifierIds();

            // Debug: Print the count of pacifiers
             //MessageBox.Show($"Loaded {pacifierIds.Count} pacifiers.");

            foreach (var pacifierId in pacifierIds)
            {
                
                // Check if the pacifier is already in the list based on PacifierId
                if (!connectedPacifiers.Any(pacifier => pacifier.PacifierId == pacifierId))
                {
                    //Debug.WriteLine($"Loaded Pacifier {pacifierId}.");
                    var connectedPacifierItem = new PacifierItem(pacifierId)
                    {
                        ButtonText = $"Pacifier {pacifierId}",
                        IsChecked = false,
                        CircleText = " "
                    };

                    // Check if the toggle changed
                    connectedPacifierItem.ToggleChanged += (s, e) =>
                    {
                        if (connectedPacifierItem.IsChecked)
                        {
                            if (!selectedPacifiers.Contains(connectedPacifierItem))
                            {
                                selectedPacifiers.Add(connectedPacifierItem);
                            }
                        }
                        else
                        {
                            selectedPacifiers.Remove(connectedPacifierItem);
                        }

                        // Update CircleText for all connected pacifiers
                        UpdateCircleText();
                    };

                    Console.WriteLine($"Adding pacifier: {pacifierId}"); // Debug
                    connectedPacifiers.Add(connectedPacifierItem);
                    ConnectedPacifierPanel.Children.Add(connectedPacifierItem);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void UpdateCircleText()
        {
            // Update the CircleText for each connected pacifier based on their selection order
            for (int i = 0; i < connectedPacifiers.Count; i++)
            {
                if (connectedPacifiers[i].IsChecked)
                {
                    connectedPacifiers[i].CircleText = (selectedPacifiers.IndexOf(connectedPacifiers[i]) + 1).ToString();
                }
                else
                {
                    connectedPacifiers[i].CircleText = " ";
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateCampaign_Click(object sender, RoutedEventArgs e)
        {
            string campaignName = CampaignTextBox.Text;

            // Validate that at least one pacifier is selected and the campaign name is not empty
            if (selectedPacifiers.Count > 0 && !string.IsNullOrWhiteSpace(campaignName))
            {

                // Get the current system time as entryTime
                string entryTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Access the ILineProtocol service and call CreateFileCamp
                ILineProtocol lineProtocolService = new FileManager(); // Use dependency injection if available
                lineProtocolService.CreateFileCamp(campaignName, entryTime);

                ILineProtocol lineProtocol = new FileManager(); // Use DI if possible


                var monitoringView = new MonitoringView(selectedPacifiers, lineProtocol, campaignName);

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
