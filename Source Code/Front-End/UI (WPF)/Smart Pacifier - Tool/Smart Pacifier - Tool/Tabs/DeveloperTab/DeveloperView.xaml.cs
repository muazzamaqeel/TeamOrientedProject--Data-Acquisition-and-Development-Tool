using SmartPacifier.Interface.Services;
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
        private List<SensorData> allData = new List<SensorData>();
        private List<SensorData> currentPageData = new List<SensorData>();
        private int currentPage = 1;
        private int pageSize = 10;

        public DeveloperView(IDatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _ = LoadCampaignsAsync();  // Load campaigns asynchronously
            _ = LoadDataAsync();  // Load the table data asynchronously
        }

        // Method to load campaigns into the Campaign ComboBox
        private async Task LoadCampaignsAsync()
        {
            try
            {
                var campaigns = await _databaseService.GetCampaignsAsync(); // Fetch campaigns from the database
                Campaign.ItemsSource = campaigns;
                Campaign.SelectedIndex = 0; // Optionally select the first item by default
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading campaigns: {ex.Message}");
            }
        }

        // Method to load data into the table
        private async Task LoadDataAsync()
        {
            try
            {
                // Fetch your data from the database or any other source here
                var campaigns = await _databaseService.GetCampaignsAsync(); // Example: Fetch campaigns
                allData = campaigns.Select(c => new SensorData
                {
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Campaign = c,
                    Pacifier = "pacifier_1", // Example placeholder data
                    Sensor = "sensor_1",     // Example placeholder data
                    Value = 36.5             // Example placeholder data
                }).ToList();
                DisplayData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
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

        // Display the data on the current page
        private void DisplayData()
        {
            currentPageData = allData.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();
            DataListView.ItemsSource = currentPageData;
        }

        // ComboBox Selection Changed Event Handler
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle selection change in ComboBoxes
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
                // Confirm and delete logic
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
