using Smart_Pacifier___Tool.Components;
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

        public PacifierSelectionView()
        {
            InitializeComponent();

            // dummy pacifiers for testing
            AddDummyPacifiers(15);
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
            if (!connectedPacifiers.Contains(pacifierName))
            {
                connectedPacifiers.Add(pacifierName);

                var connectedPacifierItem = new Components.ConnectedPacifierItem
                {
                    ButtonText = pacifierName,
                    IsChecked = false
                };

                connectedPacifierItem.Toggled += (s, e) =>
                {
                    if (connectedPacifierItem.IsChecked)
                    {
                        // Add pacifier to the list if checked
                        if (!selectedPacifiers.Contains(pacifierName))
                        {
                            selectedPacifiers.Add(pacifierName);
                        }
                    }
                    else
                    {
                        // Remove pacifier from the list if unchecked
                        selectedPacifiers.Remove(pacifierName);
                    }
                };

                ConnectedPacifierPanel.Children.Add(connectedPacifierItem);
            }
        }

        private void CreateCampaign_Click(object sender, RoutedEventArgs e)
        {
            string campaignName = CampaignTextBox.Text;

            if (selectedPacifiers.Count > 0 && !string.IsNullOrWhiteSpace(campaignName))
            {
                //MessageBox.Show($"Campaign '{campaignName}' created with pacifiers: {string.Join(", ", selectedPacifiers)}");

                var monitoringView = new MonitoringView();

                var parent = this.Parent as ContentControl;
                if (parent != null)
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
