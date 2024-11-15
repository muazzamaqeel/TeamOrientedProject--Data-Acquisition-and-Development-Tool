using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.AlgorithmTab
{
    public partial class AlgorithmView : UserControl, INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private readonly IManagerCampaign _managerCampaign;

        public ObservableCollection<Campaign> Campaigns { get; set; } = new ObservableCollection<Campaign>();
        private Dictionary<string, Campaign> campaignDataMap = new Dictionary<string, Campaign>();

        public AlgorithmView(IDatabaseService databaseService, IManagerCampaign managerCampaign)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _managerCampaign = managerCampaign;

            DataContext = this;

            LoadCampaignData();
        }

        private async void LoadCampaignData()
        {
            var csvData = await _managerCampaign.GetCampaignDataAsCSVAsync();
            var lines = csvData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            campaignDataMap.Clear();
            StringBuilder outputLog = new StringBuilder("Processing Campaign Data:\n");

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var columns = line.Split(',');

                if (columns.Length < 4) continue;

                var campaignName = columns[0];
                var status = columns[1];
                var startTimeStr = columns[2];
                var endTimeStr = columns[3];

                if (!campaignDataMap.ContainsKey(campaignName))
                {
                    campaignDataMap[campaignName] = new Campaign
                    {
                        CampaignName = campaignName,
                        PacifierCount = 0,
                        Date = "N/A",
                        TimeRange = "N/A"
                    };
                }

                var campaign = campaignDataMap[campaignName];
                outputLog.AppendLine($"Processing {campaignName} with status {status}");

                // Format and set TimeRange based on start and end times
                DateTime.TryParse(startTimeStr, out var campaignStart);
                DateTime.TryParse(endTimeStr, out var campaignEnd);

                if (!string.IsNullOrWhiteSpace(startTimeStr) && !string.IsNullOrWhiteSpace(endTimeStr))
                {
                    campaign.TimeRange = $"{campaignStart:MM/dd/yyyy HH:mm:ss} - {campaignEnd:MM/dd/yyyy HH:mm:ss}";
                }
                else if (!string.IsNullOrWhiteSpace(startTimeStr))
                {
                    campaign.TimeRange = $"{campaignStart:MM/dd/yyyy HH:mm:ss} - N/A";
                }
                else if (!string.IsNullOrWhiteSpace(endTimeStr))
                {
                    campaign.TimeRange = $"N/A - {campaignEnd:MM/dd/yyyy HH:mm:ss}";
                }
            }

            UpdateCampaignsList();
        }

        private void UpdateCampaignsList()
        {
            Campaigns.Clear();
            foreach (var campaign in campaignDataMap.Values)
            {
                Campaigns.Add(campaign);
            }

            OnPropertyChanged(nameof(Campaigns));
        }

        private void CampaignButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Campaign selectedCampaign)
            {
                // Navigate to AlgorithmsInternal, passing the campaign name and database service
                var algorithmsInternal = new AlgoDatabase(selectedCampaign.CampaignName, _databaseService);

                // Get a reference to MainWindow
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    // Use NavigateTo to replace the content in ContentArea with AlgorithmsInternal
                    mainWindow.NavigateTo(algorithmsInternal);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Define the Campaign class
    public class Campaign
    {
        public string CampaignName { get; set; } = string.Empty;
        public int PacifierCount { get; set; }
        public string Date { get; set; } = string.Empty;
        public string TimeRange { get; set; } = string.Empty;
    }
}
