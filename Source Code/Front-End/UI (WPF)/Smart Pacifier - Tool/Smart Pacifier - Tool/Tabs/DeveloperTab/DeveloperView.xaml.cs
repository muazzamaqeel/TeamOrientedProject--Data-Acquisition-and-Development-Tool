using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Tabs.DeveloperTab
{
    public partial class DeveloperView : UserControl
    {
        private readonly IDatabaseService _databaseService;  // Injecting the database service
        private List<SensorData> allData = new List<SensorData>();
        private List<SensorData> currentPageData = new List<SensorData>();
        private int currentPage = 1;
        private int pageSize = 10;

        // Constructor with dependency injection
        public DeveloperView(IDatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService; // Store the injected database service
            LoadDataAsync(); // Make this method async to allow fetching data from the database
        }

        // Load data into allData (example of async data fetching)
        private async Task LoadDataAsync()
        {
            try
            {
                // Replace this with actual data retrieval logic
                var campaigns = await _databaseService.GetCampaignsAsync(); // Example of async database call
                allData = campaigns.Select(c => new SensorData
                {
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Campaign = c,
                    Pacifier = "pacifier_1",  // Mock value
                    Sensor = "sensor_1",      // Mock value
                    Value = 36.5              // Mock value
                }).ToList();
                DisplayData(); // Display the data after loading
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        // Display the current page of data
        private void DisplayData()
        {
            currentPageData = allData?.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList() ?? new List<SensorData>();
            DataListView.ItemsSource = currentPageData;
        }

        // Pagination: Previous Button
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                DisplayData();
            }
        }

        // Pagination: Next Button
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage * pageSize < (allData?.Count ?? 0))
            {
                currentPage++;
                DisplayData();
            }
        }

        // Apply filters with null checks to avoid CS8602
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var filteredData = allData?.Where(d => d.Campaign == Campaign.SelectedItem?.ToString() &&
                                                   d.Pacifier == Pacifier.SelectedItem?.ToString() &&
                                                   d.Sensor == Sensor.SelectedItem?.ToString()).ToList();

            DataListView.ItemsSource = filteredData ?? new List<SensorData>();
        }

        // Delete selected entries with null check
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = DataListView.SelectedItems?.Cast<SensorData>().ToList();

            if (selectedItems != null && selectedItems.Count > 0)
            {
                // Show confirmation dialog
                MessageBoxResult result = MessageBox.Show("Are you sure you want to delete the selected data?",
                                                          "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                // If the user confirms deletion
                if (result == MessageBoxResult.Yes)
                {
                    foreach (var item in selectedItems)
                    {
                        allData?.Remove(item);
                    }

                    // Refresh the display
                    DisplayData();
                }
                // If Cancel is chosen, do nothing.
            }
            else
            {
                MessageBox.Show("Please select at least one entry to delete.");
            }
        }

        // Add button click
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddDataWindow addDataWindow = new AddDataWindow();

            // Open the window as a dialog
            if (addDataWindow.ShowDialog() == true)
            {
                // Add the new data to the list if Save was clicked
                var newData = new SensorData
                {
                    Timestamp = addDataWindow.Timestamp,
                    Pacifier = addDataWindow.Pacifier,
                    Campaign = addDataWindow.Campaign,
                    Sensor = addDataWindow.Sensor,
                    Value = addDataWindow.Value
                };

                allData.Add(newData);
                DisplayData();
            }
        }

        // Edit button click
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataListView.SelectedItems.Count == 1)
            {
                // Get the selected item
                var selectedItem = (SensorData)DataListView.SelectedItem;

                // Open the Edit Data Window with the selected item's data
                EditDataWindow editDataWindow = new EditDataWindow(selectedItem);

                // If Save is clicked, update the entry with the new data
                if (editDataWindow.ShowDialog() == true)
                {
                    // Update the selected item with new values
                    selectedItem.Timestamp = editDataWindow.Timestamp;
                    selectedItem.Pacifier = editDataWindow.Pacifier;
                    selectedItem.Campaign = editDataWindow.Campaign;
                    selectedItem.Sensor = editDataWindow.Sensor;
                    selectedItem.Value = editDataWindow.Value;

                    // Refresh the data display
                    DisplayData();
                }
            }
            else
            {
                MessageBox.Show("Please select one entry to edit.");
            }
        }

        // Event handler when "Select All" checkbox is checked
        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Select all items in the DataListView
            foreach (var item in DataListView.Items)
            {
                var listViewItem = DataListView.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                if (listViewItem != null)
                {
                    listViewItem.IsSelected = true;
                }
            }
        }

        // Event handler when "Select All" checkbox is unchecked
        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Deselect all items in the DataListView
            foreach (var item in DataListView.Items)
            {
                var listViewItem = DataListView.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                if (listViewItem != null)
                {
                    listViewItem.IsSelected = false;
                }
            }
        }

        // Handling placeholder-like behavior for ComboBox
        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                comboBox.SelectedIndex = -1;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null && comboBox.SelectedItem == null)
            {
                comboBox.SelectedIndex = -1; // Reset selection if nothing is chosen
            }
        }
    }

    // Example data model for sensor data
    public class SensorData
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Campaign { get; set; } = string.Empty;
        public string Pacifier { get; set; } = string.Empty;
        public string Sensor { get; set; } = string.Empty;
        public double Value { get; set; }
    }
}
