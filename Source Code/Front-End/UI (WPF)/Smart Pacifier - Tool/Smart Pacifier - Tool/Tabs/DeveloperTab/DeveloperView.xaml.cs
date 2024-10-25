using SmartPacifier.Interface.Services;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Tabs.DeveloperTab
{
    public partial class DeveloperView : UserControl
    {
        private readonly IDatabaseService _databaseService;
        private readonly IManagerPacifiers _managerPacifiers;
        private List<SensorData> allData = new List<SensorData>();
        private List<SensorData> currentPageData = new List<SensorData>();
        private int currentPage = 1;
        private int pageSize = 10;

        public DeveloperView(IDatabaseService databaseService, IManagerPacifiers managerPacifiers)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _managerPacifiers = managerPacifiers;
            _ = LoadDataAsync();  // Call the async method but don't await, suppressing CS4014
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var campaigns = await _databaseService.GetCampaignsAsync();
                Campaign.ItemsSource = campaigns;

                allData = campaigns.Select(c => new SensorData
                {
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Campaign = c,
                    Pacifier = "pacifier_1",
                    Sensor = "sensor_1",
                    Value = 36.5
                }).ToList();
                DisplayData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        private void DisplayData()
        {
            currentPageData = allData.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();
            DataListView.ItemsSource = currentPageData;
        }

        // Apply Button Click Event Handler
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var filteredData = allData.Where(d =>
                d.Campaign == Campaign.SelectedItem?.ToString() &&
                d.Pacifier == Pacifier.SelectedItem?.ToString() &&
                d.Sensor == Sensor.SelectedItem?.ToString()).ToList();

            DataListView.ItemsSource = filteredData;
        }

        // Pagination: Previous Button Click Event Handler
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                DisplayData();
            }
        }

        // Pagination: Next Button Click Event Handler
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage * pageSize < allData.Count)
            {
                currentPage++;
                DisplayData();
            }
        }

        // ComboBox Selection Changed Event Handler
        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == Campaign)
            {
                var selectedCampaign = Campaign.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedCampaign))
                {
                    var pacifiers = await _managerPacifiers.GetPacifiersAsync(selectedCampaign);
                    Pacifier.ItemsSource = pacifiers;
                }
            }
        }

        // Add Button Click Event Handler
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle adding data logic
        }

        // Edit Button Click Event Handler
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataListView.SelectedItems.Count == 1)
            {
                var selectedItem = (SensorData)DataListView.SelectedItem;
                // Implement edit logic
            }
            else
            {
                MessageBox.Show("Please select one entry to edit.");
            }
        }

        // Delete Button Click Event Handler
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = DataListView.SelectedItems.Cast<SensorData>().ToList();
            if (selectedItems.Any())
            {
                // Confirm and delete
            }
            else
            {
                MessageBox.Show("Please select at least one entry to delete.");
            }
        }

        // Select All CheckBox Checked Event Handler
        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in DataListView.Items)
            {
                var listViewItem = DataListView.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                if (listViewItem != null)
                {
                    listViewItem.IsSelected = true;
                }
            }
        }

        // Select All CheckBox Unchecked Event Handler
        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in DataListView.Items)
            {
                var listViewItem = DataListView.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                if (listViewItem != null)
                {
                    listViewItem.IsSelected = false;
                }
            }
        }
    }

    public class SensorData
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Campaign { get; set; } = string.Empty;
        public string Pacifier { get; set; } = string.Empty;
        public string Sensor { get; set; } = string.Empty;
        public double Value { get; set; }
    }
}
