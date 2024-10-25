using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Smart_Pacifier___Tool.Tabs.MonitoringTab;

namespace Smart_Pacifier___Tool.Tabs.CampaignsTab
{
    public partial class CampaignsView : UserControl, INotifyPropertyChanged
    {
        private ICollectionView _filteredCampaigns;
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

        public CampaignsView()
        {
            InitializeComponent();
            GenerateCampaigns();
            FilteredCampaigns = CollectionViewSource.GetDefaultView(Campaigns);
            FilteredCampaigns.Filter = FilterCampaigns;
            FilteredCampaigns.Refresh();
            DataContext = this; // Set DataContext for binding
            isLoaded = true;
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