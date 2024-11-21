using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.DataManipulation;
using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Smart_Pacifier___Tool.Tabs.DeveloperTab
{
    public partial class DeveloperView : UserControl
    {
        private readonly IDatabaseService _databaseService;
        private readonly IManagerPacifiers _managerPacifiers;
        private readonly IManagerCampaign _managerCampaign;
        private readonly IDataManipulationHandler _dataManipulationHandler;

        private DataTable _allData = new DataTable();
        private int _currentPage = 1;
        private const int PageSize = 50; // Number of rows per page

        // Cached ComboBox data
        private static List<string> _cachedCampaigns;
        private static List<string> _cachedPacifiers;
        private static List<string> _cachedSensors;

        public DeveloperView(IDatabaseService databaseService, IManagerCampaign managerCampaign, IManagerPacifiers managerPacifiers)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _managerPacifiers = managerPacifiers;
            _managerCampaign = managerCampaign;

            _dataManipulationHandler = new DataManipulationHandler(_databaseService);

            // Load data asynchronously
            this.Loaded += DeveloperView_Loaded;
        }

        // Event handler for UserControl Loaded event
        private async void DeveloperView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        // Asynchronously load data
        private async Task LoadDataAsync()
        {
            try
            {
                ShowLoadingSpinner();

                // Fetch all data asynchronously
                _allData = await _databaseService.GetSensorDataAsync();

                // Display the first page
                DisplayData(_allData, _currentPage);

                // Populate combo boxes asynchronously
                await PopulateComboBoxesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideLoadingSpinner();
            }
        }

        // Display data with pagination
        private void DisplayData(DataTable data, int page)
        {
            if (data.Rows.Count == 0)
            {
                Dispatcher.Invoke(() =>
                {
                    DataListView.ItemsSource = null;
                    MessageBox.Show("No data to display.");
                    PageIndicator.Text = "Page 0";
                });
                return;
            }

            // Calculate the range of rows for the current page
            int startRow = (page - 1) * PageSize;
            int endRow = Math.Min(startRow + PageSize, data.Rows.Count);

            // Extract rows for the current page
            DataTable paginatedData;
            if (startRow < data.Rows.Count)
            {
                var rows = data.AsEnumerable().Skip(startRow).Take(endRow - startRow);
                paginatedData = rows.CopyToDataTable();
            }
            else
            {
                paginatedData = data.Clone(); // Empty DataTable
            }

            Dispatcher.Invoke(() =>
            {
                DataListView.ItemsSource = paginatedData.DefaultView;
                PageIndicator.Text = $"Page {page} of {Math.Ceiling((double)data.Rows.Count / PageSize)}";
            });

            // Generate columns dynamically only once
            if (page == 1)
            {
                GenerateColumns(data);
            }
        }

        // Generate GridView columns dynamically based on DataTable columns
        private void GenerateColumns(DataTable dataTable)
        {
            Dispatcher.Invoke(() =>
            {
                DataGridView.Columns.Clear();

                // Add checkbox column
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

                ((CheckBox)checkBoxColumn.Header).Checked += SelectAllCheckBox_Checked;
                ((CheckBox)checkBoxColumn.Header).Unchecked += SelectAllCheckBox_Unchecked;

                DataGridView.Columns.Add(checkBoxColumn);

                // Add other columns
                foreach (DataColumn column in dataTable.Columns)
                {
                    var gridViewColumn = new GridViewColumn
                    {
                        Header = column.ColumnName,
                        DisplayMemberBinding = new Binding($"[{column.ColumnName}]"),
                        Width = 150 // Set a default width
                    };

                    DataGridView.Columns.Add(gridViewColumn);
                }
            });
        }

        // Populate ComboBoxes with unique values
        private async Task PopulateComboBoxesAsync()
        {
            try
            {
                await PreloadComboBoxDataAsync();

                Dispatcher.Invoke(() =>
                {
                    PopulateComboBoxesFromCache();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error populating combo boxes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Preload ComboBox data and cache it
        private async Task PreloadComboBoxDataAsync()
        {
            if (_cachedCampaigns == null || _cachedPacifiers == null || _cachedSensors == null)
            {
                var campaignsTask = _databaseService.GetUniqueCampaignNamesAsync();
                var pacifiersTask = _databaseService.GetUniquePacifierNamesAsync();
                var sensorsTask = _databaseService.GetUniqueSensorTypesAsync();

                var results = await Task.WhenAll(campaignsTask, pacifiersTask, sensorsTask);

                _cachedCampaigns = results[0];
                _cachedPacifiers = results[1];
                _cachedSensors = results[2];
            }
        }

        // Populate ComboBoxes from cached data
        private void PopulateComboBoxesFromCache()
        {
            Campaign.ItemsSource = new List<string> { "All" }.Concat(_cachedCampaigns);
            Pacifier.ItemsSource = new List<string> { "All" }.Concat(_cachedPacifiers);
            Sensor.ItemsSource = new List<string> { "All" }.Concat(_cachedSensors);

            Campaign.SelectedIndex = 0;
            Pacifier.SelectedIndex = 0;
            Sensor.SelectedIndex = 0;
        }

        // Show loading spinner
        private void ShowLoadingSpinner()
        {
            LoadingSpinner.Visibility = Visibility.Visible;
            DataListView.Visibility = Visibility.Collapsed;
        }

        // Hide loading spinner
        private void HideLoadingSpinner()
        {
            LoadingSpinner.Visibility = Visibility.Collapsed;
            DataListView.Visibility = Visibility.Visible;
        }

        // Event handler for ComboBox SelectionChanged
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Apply filters based on ComboBox selections
        private void ApplyFilters()
        {
            if (_allData == null || _allData.Columns.Count == 0)
            {
                MessageBox.Show("No data available to filter.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string selectedCampaign = Campaign.SelectedItem as string ?? "All";
            string selectedPacifier = Pacifier.SelectedItem as string ?? "All";
            string selectedSensorType = Sensor.SelectedItem as string ?? "All";

            // Dynamically handle column names
            string campaignColumn = _allData.Columns.Contains("campaign_name") ? "campaign_name" : null;
            string pacifierColumn = _allData.Columns.Contains("pacifier_name") ? "pacifier_name" : null;
            string sensorColumn = _allData.Columns.Contains("sensor_type") ? "sensor_type" : null;

            var filteredData = _allData.AsEnumerable().Where(row =>
                (selectedCampaign == "All" || (campaignColumn != null && row[campaignColumn].ToString() == selectedCampaign)) &&
                (selectedPacifier == "All" || (pacifierColumn != null && row[pacifierColumn].ToString() == selectedPacifier)) &&
                (selectedSensorType == "All" || (sensorColumn != null && row[sensorColumn].ToString() == selectedSensorType)));

            if (filteredData.Any())
            {
                DataTable filteredTable = filteredData.CopyToDataTable();
                _currentPage = 1; // Reset to first page when filters are applied
                DisplayData(filteredTable, _currentPage);
            }
            else
            {
                DataListView.ItemsSource = null;
                MessageBox.Show("No data matches the selected filters.");
                PageIndicator.Text = "Page 0";
            }
        }

        // Event handler for Previous button
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                DisplayData(_allData, _currentPage);
            }
        }

        // Event handler for Next button
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage * PageSize < _allData.Rows.Count)
            {
                _currentPage++;
                DisplayData(_allData, _currentPage);
            }
        }

        // Event handler for Add button
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Implement your add functionality here
            MessageBox.Show("Add functionality is not yet implemented.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Event handler for Edit button
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
                MessageBox.Show("Please select a row to edit.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Event handler for Delete button
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
                            MessageBox.Show("Error: 'entry_id' or 'Measurement' column not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    MessageBox.Show("Selected entries deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Reload data to reflect changes
                    await LoadDataAsync();
                }
                else
                {
                    MessageBox.Show("Please select entries to delete.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting entry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event handler for Select All checkbox checked
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

        // Event handler for Select All checkbox unchecked
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
