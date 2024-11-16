using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Smart_Pacifier___Tool.Components;
using Smart_Pacifier___Tool.Tabs.MonitoringTab;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.CampaignsTab
{
    public partial class CampaignsView : UserControl, INotifyPropertyChanged
    {
        private ICollectionView _filteredCampaigns;
        private readonly IDatabaseService _databaseService;
        private readonly IManagerCampaign _managerCampaign; // 
        public List<Campaign> Campaigns { get; set; } = new List<Campaign>();
        public string SearchName { get; set; } = string.Empty;
        public string ActualSearchName { get; set; } = string.Empty; // Newa property
        public DateTime? SearchDate { get; set; }
        public bool isLoaded = false;
        private Dictionary<string, Campaign> campaignDataMap = new Dictionary<string, Campaign>(); // Define campaignDataMap at class level

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

            LoadCampaignsData();
            FilteredCampaigns = CollectionViewSource.GetDefaultView(Campaigns);
            FilteredCampaigns.Filter = FilterCampaigns;
            FilteredCampaigns.Refresh();
            DataContext = this;
            isLoaded = true;
        }

        private async void LoadCampaignsData()
        {
            LoadingSpinner.Visibility = Visibility.Visible; // Show the spinner

            var campaigns = await _managerCampaign.GetCampaignsDataAsync();
            foreach (var campaign in campaigns)
            {
                var parts = campaign.Split(new[] { "Campaign: ", ", PacifierCount: " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var campaignName = parts[0];
                    if (int.TryParse(parts[1], out int pacifierCount))
                    {
                        Campaigns.Add(new Campaign
                        {
                            CampaignName = campaignName,
                            PacifierCount = pacifierCount,
                            //Date = DateTime.Now.ToString("MM/dd/yyyy"), // Placeholder date
                            //TimeRange = $"{DateTime.Now.ToString("hh:mm tt")} - {DateTime.Now.AddHours(1).ToString("hh:mm tt")}" // Placeholder time range
                        });
                    }
                }
            }

            FilteredCampaigns.Refresh();
            LoadingSpinner.Visibility = Visibility.Collapsed; // Hide the spinner
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
            TextBox? textBox = sender as TextBox;
            if (textBox.Text == "Search by name")
            {
                textBox.Text = "";
                textBox.Foreground = (SolidColorBrush)Application.Current.Resources["MainViewForegroundColor"];
            }
        }

        private void AddPlaceholderText(object sender, RoutedEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
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
            TextBox ?textBox = sender as TextBox;
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
            DatePicker ?datePicker = sender as DatePicker;
            if (datePicker != null)
            {
                SearchDate = datePicker.SelectedDate;
            }
            FilteredCampaigns.Refresh();
        }

        private void DatePicker_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            DatePicker ?datePicker = sender as DatePicker;
            if (datePicker != null && string.IsNullOrEmpty(datePicker.Text))
            {
                SearchDate = null;
                FilteredCampaigns.Refresh();
            }
        }

        private void Campaign_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Campaign selectedCampaign)
            {
                // Create an instance of CampaignsInternal, passing the database singleton and campaign name
                var campaignsInternal = new CampaignsInternal(_managerCampaign, selectedCampaign.CampaignName);

                // Get a reference to MainWindow
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    // Use NavigateTo to replace the content in MainContent with CampaignsInternal
                    mainWindow.NavigateTo(new CampaignView(_managerCampaign, selectedCampaign.CampaignName));
                }
            }
        }



        public event PropertyChangedEventHandler? PropertyChanged;

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