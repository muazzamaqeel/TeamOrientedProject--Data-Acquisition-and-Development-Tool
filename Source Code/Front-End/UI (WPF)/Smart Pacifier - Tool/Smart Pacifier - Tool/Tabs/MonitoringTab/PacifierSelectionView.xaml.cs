using Smart_Pacifier___Tool.Components;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using SmartPacifier.Interface.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Smart_Pacifier___Tool.Tabs.MonitoringTab
{
    public partial class PacifierSelectionView : UserControl
    {
        private List<string> connectedPacifiers = new List<string>();
        private List<string> selectedPacifiers = new List<string>();
        private readonly IManagerPacifiers _managerPacifiers;


        public PacifierSelectionView(IManagerPacifiers managerPacifiers)
        {
            InitializeComponent();
            _managerPacifiers = managerPacifiers;

            // dummy pacifiers for testing
            LoadPacifierNames();
        }


        private void LoadPacifierNames()
        {
            var pacifierNames = _managerPacifiers.GetPacifierNamesFromSensorData();

            // Debug: Print the count of pacifiers
            MessageBox.Show($"Loaded {pacifierNames.Count} pacifiers.");

            foreach (var pacifierName in pacifierNames)
            {
                AddConnectedPacifier(pacifierName);
            }
        }


        // TESTING - to be removed later
        private void AddDummyPacifiers(int number)
        {
            for (int i = 1; i <= number; i++)
            {
                AddConnectedPacifier($"Pacifier {i}");
            }
        }

        public void AddConnectedPacifier(string pacifierName)
        {
            var connectedPacifierItem = new ConnectedPacifierItem
            {
                ButtonText = pacifierName,
                IsChecked = false
            };

            connectedPacifierItem.Toggled += (s, e) =>
            {
                if (connectedPacifierItem.IsChecked)
                {
                    if (!selectedPacifiers.Contains(pacifierName))
                    {
                        selectedPacifiers.Add(pacifierName);
                        //MessageBox.Show($"{pacifierName} added to selected list.");
                    }
                }
                else
                {
                    selectedPacifiers.Remove(pacifierName);
                    //MessageBox.Show($"{pacifierName} removed from selected list.");
                }
            };



            Console.WriteLine($"Adding pacifier: {pacifierName}"); // Debug
            ConnectedPacifierPanel.Children.Add(connectedPacifierItem);
        }


        private void CreateCampaign_Click(object sender, RoutedEventArgs e)
        {
            string campaignName = CampaignTextBox.Text;

            // Debug message to check campaign name
            //MessageBox.Show($"Campaign Name: {campaignName}");
            //MessageBox.Show($"Selected Pacifiers: {string.Join(", ", selectedPacifiers)} (Count: {selectedPacifiers.Count})");


            // Validate that at least one pacifier is selected and the campaign name is not empty
            if (selectedPacifiers.Count > 0 && !string.IsNullOrWhiteSpace(campaignName))
            {
                var monitoringView = new MonitoringView();

                // Optionally pass selected pacifiers to the monitoring view

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
