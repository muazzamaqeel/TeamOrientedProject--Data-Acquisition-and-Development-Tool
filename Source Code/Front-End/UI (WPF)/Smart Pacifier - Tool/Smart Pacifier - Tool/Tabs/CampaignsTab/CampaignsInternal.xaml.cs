using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.CampaignsTab
{
    /// <summary>
    /// Interaction logic for CampaignsInternal.xaml
    /// </summary>
    public partial class CampaignsInternal : UserControl
    {
        private readonly IManagerCampaign _managerCampaign;
        private readonly string _campaignName;

        // Constructor accepting the database manager and campaign name
        public CampaignsInternal(IManagerCampaign managerCampaign, string campaignName)
        {
            InitializeComponent();
            _managerCampaign = managerCampaign;
            _campaignName = campaignName;

            // Debug to confirm the campaign name is passed correctly
            //Debug.WriteLine($"CampaignsInternal initialized with CampaignName: {_campaignName}");

            // Update the UI with the campaign name
            CampaignTitle.Text = $"{_campaignName}";
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
