using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.DataManipulation;
using System.Windows.Data;
using System.Windows.Input;

namespace Smart_Pacifier___Tool.Tabs.DeveloperTab
{
    public partial class DeveloperView : UserControl
    {
        private readonly IDatabaseService _databaseService;
        private readonly IManagerPacifiers _managerPacifiers;
        private readonly IManagerCampaign _managerCampaign;
        private readonly IDataManipulationHandler _dataManipulationHandler;

        private DataTable allData = new DataTable();
        private int currentPage = 1;
        private int pageSize = 10;

        public DeveloperView(IDatabaseService databaseService, IManagerCampaign managerCampaign, IManagerPacifiers managerPacifiers)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _managerPacifiers = managerPacifiers;
            _managerCampaign = managerCampaign;

            _dataManipulationHandler = new DataManipulationHandler(_databaseService);

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Load all sensor data from the database
                allData.Clear();
                allData = await _databaseService.GetSensorDataAsync();

                // Extract unique values for Campaign, Pacifier, and Sensor Type for combo boxes
                var uniqueCampaigns = allData.Columns.Contains("campaign_name") ? allData.AsEnumerable()
                    .Select(row => row["campaign_name"].ToString())
                    .Distinct()
                    .ToList() : new List<string>();

                var uniquePacifiers = allData.Columns.Contains("pacifier_name") ? allData.AsEnumerable()
                    .Select(row => row["pacifier_name"].ToString())
                    .Distinct()
                    .ToList() : new List<string>();

                var uniqueSensors = allData.Columns.Contains("sensor_type") ? allData.AsEnumerable()
                    .Select(row => row["sensor_type"].ToString())
                    .Distinct()
                    .ToList() : new List<string>();

                // Populate the combo boxes with unique values
                Campaign.ItemsSource = uniqueCampaigns;
                Pacifier.ItemsSource = uniquePacifiers;
                Sensor.ItemsSource = uniqueSensors;

                // Display all data initially
                DisplayData(allData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        private void DisplayData(DataTable dataToDisplay)
        {
            if (dataToDisplay.Rows.Count == 0)
            {
                MessageBox.Show("No data to display.");
                return;
            }

            // Clear existing columns
            DataGridView.Columns.Clear();

            // Add the checkbox column
            var checkBoxColumn = new GridViewColumn
            {
                Header = new CheckBox
                {
                    Name = "SelectAllCheckBox",
                    IsThreeState = false,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
                CellTemplate = FindResource("CheckBoxCellTemplate") as DataTemplate,
                Width = 30
            };

            // Attach event handlers for the SelectAll checkbox
            ((CheckBox)checkBoxColumn.Header).Checked += SelectAllCheckBox_Checked;
            ((CheckBox)checkBoxColumn.Header).Unchecked += SelectAllCheckBox_Unchecked;

            DataGridView.Columns.Add(checkBoxColumn);

            // Generate columns dynamically based on DataTable columns
            foreach (DataColumn column in dataToDisplay.Columns)
            {
                // Skip columns that are not needed or already added
                if (DataGridView.Columns.Any(c => c.Header.ToString() == column.ColumnName))
                    continue;

                // Create a new GridViewColumn
                var gridViewColumn = new GridViewColumn
                {
                    Header = column.ColumnName,
                    DisplayMemberBinding = new Binding($"[{column.ColumnName}]"),
                    Width = Double.NaN // Auto width
                };

                DataGridView.Columns.Add(gridViewColumn);
            }

            // Set the ItemsSource of the ListView
            DataListView.ItemsSource = dataToDisplay.DefaultView;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedCampaign = Campaign.SelectedItem?.ToString();
            string selectedPacifier = Pacifier.SelectedItem?.ToString();
            string selectedSensorType = Sensor.SelectedItem?.ToString();

            var filteredData = allData.AsEnumerable().Where(row =>
                (string.IsNullOrEmpty(selectedCampaign) || (allData.Columns.Contains("campaign_name") && row["campaign_name"].ToString() == selectedCampaign)) &&
                (string.IsNullOrEmpty(selectedPacifier) || (allData.Columns.Contains("pacifier_name") && row["pacifier_name"].ToString() == selectedPacifier)) &&
                (string.IsNullOrEmpty(selectedSensorType) || (allData.Columns.Contains("sensor_type") && row["sensor_type"].ToString() == selectedSensorType)));

            if (filteredData.Any())
            {
                DataTable filteredTable = filteredData.CopyToDataTable();
                DisplayData(filteredTable);
            }
            else
            {
                DataListView.ItemsSource = null;
                MessageBox.Show("No data matches the selected filters.");
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                DisplayData(allData);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage * pageSize < allData.Rows.Count)
            {
                currentPage++;
                DisplayData(allData);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Do nothing here, Apply button will handle filtering
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Implement your add functionality here
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataListView.SelectedItem is DataRowView selectedRow)
            {
                // Extract the selected row data into a dictionary
                var originalData = selectedRow.Row
                    .Table.Columns.Cast<DataColumn>()
                    .ToDictionary(col => col.ColumnName, col => selectedRow.Row[col] ?? string.Empty); // Ensure no null values

                // Create an instance of the EditDataWindow and pass the data
                var editWindow = new EditDataWindow(_dataManipulationHandler, originalData);

                // Show the EditDataWindow as a dialog and refresh data if changes were saved
                if (editWindow.ShowDialog() == true)
                {
                    await LoadDataAsync();
                }
            }
            else
            {
                MessageBox.Show("Please select a row to edit.");
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataListView.SelectedItems.Count > 0)
                {
                    var itemsToDelete = DataListView.SelectedItems.Cast<DataRowView>().ToList();

                    foreach (var selectedRow in itemsToDelete)
                    {
                        // Check for the exact name of the column
                        if (selectedRow.Row.Table.Columns.Contains("entry_id") && selectedRow.Row.Table.Columns.Contains("Measurement"))
                        {
                            // Retrieve the entry ID and measurement type from the selected row
                            int entryId = Convert.ToInt32(selectedRow["entry_id"]);
                            string measurement = selectedRow["Measurement"].ToString();

                            // Call the delete method with both entryId and measurement
                            await _databaseService.DeleteEntryFromDatabaseAsync(entryId, measurement);
                        }
                        else
                        {
                            MessageBox.Show("Error: 'entry_id' or 'Measurement' column not found.");
                        }
                    }

                    MessageBox.Show("Selected entries deleted successfully.");

                    // Reload data to reflect changes
                    await LoadDataAsync();
                }
                else
                {
                    MessageBox.Show("Please select entries to delete.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting entry: {ex.Message}");
            }
        }

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
}
