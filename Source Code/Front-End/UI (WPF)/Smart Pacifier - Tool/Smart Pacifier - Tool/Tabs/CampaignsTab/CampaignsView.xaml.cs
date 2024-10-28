using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Smart_Pacifier___Tool.Tabs.MonitoringTab;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.CampaignsTab
{
    public partial class CampaignsView : UserControl, INotifyPropertyChanged
    {
        private ICollectionView _filteredCampaigns;
        private readonly IDatabaseService _databaseService;
        private readonly IManagerCampaign _managerCampaign;
        public List<Campaign> Campaigns { get; set; } = new List<Campaign>();
        public string SearchName { get; set; } = string.Empty;
        public string ActualSearchName { get; set; } = string.Empty; // New property
        public DateTime? SearchDate { get; set; }
        public bool isLoaded = false;

        public ICollectionView FilteredCampaigns
        {
            get => _filteredCampaigns;
            set
            {
                _filteredCampaigns = value;
                OnPropertyChanged(nameof(FilteredCampaigns));
            }
        }

        // Use the dependency injection here
        public CampaignsView(IDatabaseService databaseService, IManagerCampaign managerCampaign)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _managerCampaign = managerCampaign;

            LoadCampaignData();
            FilteredCampaigns = CollectionViewSource.GetDefaultView(Campaigns);
            FilteredCampaigns.Filter = FilterCampaigns;
            FilteredCampaigns.Refresh();
            DataContext = this;
            isLoaded = true;
        }
        private async void LoadCampaignData()
        {
            var csvData = await _managerCampaign.GetCampaignDataAsCSVAsync();
            var lines = csvData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            // Dictionary to hold campaign data by unique campaign names
            var campaignDataMap = new Dictionary<string, Campaign>();
            StringBuilder outputLog = new StringBuilder("Processing Campaign Data:\n");

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var columns = line.Split(',');

                // Ensure we have enough columns for parsing
                if (columns.Length < 4) continue;

                var campaignName = columns[0];
                var status = columns[1];
                var startTimeStr = columns[2];
                var endTimeStr = columns[3];

                // Get or create the Campaign object for this campaign name
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

                // Set values based on status
                if (status == "created" && DateTime.TryParse(startTimeStr, out var creationDate))
                {
                    campaign.Date = creationDate.ToString("MM/dd/yyyy");
                }
                else if (status == "started" && DateTime.TryParse(startTimeStr, out var campaignStart))
                {
                    campaign.TimeRange = $"{campaignStart.ToString("hh:mm tt")} -";
                }
                else if (status == "stopped" && DateTime.TryParse(endTimeStr, out var campaignEnd))
                {
                    if (campaign.TimeRange.EndsWith("-"))
                    {
                        campaign.TimeRange += $" {campaignEnd.ToString("hh:mm tt")}";
                    }
                }
            }

            // Update the observable list and refresh UI
            Campaigns.Clear();
            foreach (var campaign in campaignDataMap.Values)
            {
                Campaigns.Add(campaign);
            }
            FilteredCampaigns.Refresh();

            // Display consolidated output in one message box
            MessageBox.Show(outputLog.ToString(), "Load Campaign Data Output", MessageBoxButton.OK, MessageBoxImage.Information);
        }





        private void GenerateCampaigns()
        {
            for (int i = 1; i <= 10; i++)
            {
                Campaigns.Add(new Campaign
                {
                    CampaignName = $"Campaign {i}",
                    PacifierCount = i * 10,
                    Date = DateTime.Now.AddDays(i).ToString("MM/dd/yyyy"),
                    TimeRange = $"{DateTime.Now.AddHours(i).ToString("hh:mm tt")} - {DateTime.Now.AddHours(i + 1).ToString("hh:mm tt")}"
                });
            }
        }

        private bool FilterCampaigns(object item)
        {
            if (item is Campaign campaign)
            {
                bool matchesName = string.IsNullOrEmpty(ActualSearchName) || campaign.CampaignName.Contains(ActualSearchName, StringComparison.OrdinalIgnoreCase);
                bool matchesDate = !SearchDate.HasValue || campaign.Date == SearchDate.Value.ToString("MM/dd/yyyy");
                return matchesName && matchesDate;
            }
            return false;
        }

        private void RemovePlaceholderText(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text == "Search by name")
            {
                textBox.Text = "";
                textBox.Foreground = (SolidColorBrush)Application.Current.Resources["MainViewForegroundColor"];
            }
        }

        private void AddPlaceholderText(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Search by name";
                textBox.Foreground = Brushes.Gray;
            }
            else
            {
                // Ensure the filtering logic is applied correctly
                ActualSearchName = textBox.Text;
                if (FilteredCampaigns != null)
                {
                    FilteredCampaigns.Refresh();
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && isLoaded)
            {
                ActualSearchName = textBox.Text == "Search by name" ? string.Empty : textBox.Text;
                if (FilteredCampaigns != null)
                {
                    FilteredCampaigns.Refresh();
                }
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DatePicker datePicker = sender as DatePicker;
            if (datePicker != null)
            {
                SearchDate = datePicker.SelectedDate;
            }
            FilteredCampaigns.Refresh();
        }

        private void DatePicker_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            DatePicker datePicker = sender as DatePicker;
            if (datePicker != null && string.IsNullOrEmpty(datePicker.Text))
            {
                SearchDate = null;
                FilteredCampaigns.Refresh();
            }
        }

        private void Campaign_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).NavigateTo(new MonitoringView());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Campaign
    {
        public string CampaignName { get; set; } = string.Empty;
        public int PacifierCount { get; set; }
        public string Date { get; set; } = string.Empty;
        public string TimeRange { get; set; } = string.Empty;
    }
}