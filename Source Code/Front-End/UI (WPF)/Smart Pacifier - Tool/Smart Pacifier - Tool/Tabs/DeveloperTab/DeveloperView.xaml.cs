﻿using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.DataManipulation;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
                ShowLoadingSpinner();

                // Load all sensor data from the database
                allData.Clear();
                allData = await _databaseService.GetSensorDataAsync();

                // Populate ComboBoxes with unique values from the database
                await PopulateComboBoxesAsync();

                // Display all data initially
                DisplayData(allData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
            finally
            {
                HideLoadingSpinner();
            }
        }



        private void ShowLoadingSpinner()
        {
            LoadingSpinner.Visibility = Visibility.Visible;
            DataListView.Visibility = Visibility.Collapsed;
        }

        private void HideLoadingSpinner()
        {
            LoadingSpinner.Visibility = Visibility.Collapsed;
            DataListView.Visibility = Visibility.Visible;
        }

        private async Task PopulateComboBoxesAsync()
        {
            if (allData == null || allData.Columns.Count == 0)
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                // Clear existing items
                Campaign.Items.Clear();
                Pacifier.Items.Clear();
                Sensor.Items.Clear();

                // Add "All" as default option
                Campaign.Items.Add("All");
                Pacifier.Items.Add("All");
                Sensor.Items.Add("All");

                // Populate unique values dynamically
                if (allData.Columns.Contains("campaign_name"))
                {
                    var campaigns = allData.AsEnumerable()
                        .Select(row => row["campaign_name"].ToString())
                        .Distinct()
                        .OrderBy(x => x);
                    foreach (var campaign in campaigns) Campaign.Items.Add(campaign);
                }

                if (allData.Columns.Contains("pacifier_name"))
                {
                    var pacifiers = allData.AsEnumerable()
                        .Select(row => row["pacifier_name"].ToString())
                        .Distinct()
                        .OrderBy(x => x);
                    foreach (var pacifier in pacifiers) Pacifier.Items.Add(pacifier);
                }

                if (allData.Columns.Contains("sensor_type"))
                {
                    var sensors = allData.AsEnumerable()
                        .Select(row => row["sensor_type"].ToString())
                        .Distinct()
                        .OrderBy(x => x);
                    foreach (var sensor in sensors) Sensor.Items.Add(sensor);
                }

                // Set default selection
                Campaign.SelectedIndex = 0;
                Pacifier.SelectedIndex = 0;
                Sensor.SelectedIndex = 0;
            });
        }




        private void DisplayData(DataTable dataToDisplay)
        {
            Dispatcher.Invoke(() =>
            {
                if (dataToDisplay.Rows.Count == 0)
                {
                    DataListView.ItemsSource = null;
                    MessageBox.Show("No data to display.");
                    return;
                }

                // **Add this block to print column names and sample data**
                Console.WriteLine("Columns in dataToDisplay:");
                foreach (DataColumn column in dataToDisplay.Columns)
                {
                    Console.WriteLine(column.ColumnName);
                }

                // Print first few rows as sample data
                Console.WriteLine("Sample data from dataToDisplay:");
                int maxRowsToPrint = Math.Min(5, dataToDisplay.Rows.Count);
                for (int i = 0; i < maxRowsToPrint; i++)
                {
                    DataRow row = dataToDisplay.Rows[i];
                    Console.WriteLine(string.Join(", ", row.ItemArray));
                }
                // **End of added block**

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
                    // Skip columns that are already added
                    if (DataGridView.Columns.Any(c => c.Header.ToString() == column.ColumnName))
                        continue;

                    // Create a new GridViewColumn
                    var gridViewColumn = new GridViewColumn
                    {
                        Header = column.ColumnName,
                        DisplayMemberBinding = new Binding($"[{column.ColumnName}]"),
                        Width = Double.NaN // Set to Auto width initially
                    };

                    DataGridView.Columns.Add(gridViewColumn);
                }

                // Set the ItemsSource of the ListView
                DataListView.ItemsSource = dataToDisplay.DefaultView;

                // Adjust column widths to fit content
                AutoAdjustColumnWidths(dataToDisplay);
            });
        }


        private void AutoAdjustColumnWidths(DataTable dataTable)
        {
            GridView gridView = DataGridView;

            // Exclude the checkbox column (index 0)
            for (int i = 1; i < gridView.Columns.Count; i++)
            {
                GridViewColumn column = gridView.Columns[i];
                double maxWidth = 0;

                // Measure header width
                if (column.Header != null)
                {
                    var header = new TextBlock { Text = column.Header.ToString(), FontSize = 14, FontFamily = new FontFamily("Segoe UI") };
                    header.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    if (header.DesiredSize.Width > maxWidth)
                    {
                        maxWidth = header.DesiredSize.Width;
                    }
                }

                // Measure cells width
                foreach (DataRowView rowView in DataListView.Items)
                {
                    string cellText = rowView.Row[column.Header.ToString()]?.ToString() ?? "";
                    var cell = new TextBlock { Text = cellText, FontSize = 14, FontFamily = new FontFamily("Segoe UI") };
                    cell.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    if (cell.DesiredSize.Width > maxWidth)
                    {
                        maxWidth = cell.DesiredSize.Width;
                    }
                }

                // Add padding
                column.Width = maxWidth + 20; // Add some extra space
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedCampaign = Campaign.SelectedItem as string;
            string selectedPacifier = Pacifier.SelectedItem as string;
            string selectedSensorType = Sensor.SelectedItem as string;

            var filteredData = allData.AsEnumerable().Where(row =>
                (selectedCampaign == "All" || (allData.Columns.Contains("campaign_name") && row["campaign_name"].ToString() == selectedCampaign)) &&
                (selectedPacifier == "All" || (allData.Columns.Contains("pacifier_name") && row["pacifier_name"].ToString() == selectedPacifier)) &&
                (selectedSensorType == "All" || (allData.Columns.Contains("sensor_type") && row["sensor_type"].ToString() == selectedSensorType)));

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
            ApplyFilters();
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

        private void ApplyFilters()
        {
            if (allData == null || allData.Columns.Count == 0)
            {
                MessageBox.Show("No data available to filter.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string selectedCampaign = Campaign.SelectedItem as string ?? "All";
            string selectedPacifier = Pacifier.SelectedItem as string ?? "All";
            string selectedSensorType = Sensor.SelectedItem as string ?? "All";

            // Dynamically handle column names
            string campaignColumn = allData.Columns.Contains("campaign_name") ? "campaign_name" : null;
            string pacifierColumn = allData.Columns.Contains("pacifier_name") ? "pacifier_name" : null;
            string sensorColumn = allData.Columns.Contains("sensor_type") ? "sensor_type" : null;

            var filteredData = allData.AsEnumerable().Where(row =>
                (selectedCampaign == "All" || (campaignColumn != null && row[campaignColumn].ToString() == selectedCampaign)) &&
                (selectedPacifier == "All" || (pacifierColumn != null && row[pacifierColumn].ToString() == selectedPacifier)) &&
                (selectedSensorType == "All" || (sensorColumn != null && row[sensorColumn].ToString() == selectedSensorType)));

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


        private void DeveloperView_Loaded(object sender, RoutedEventArgs e)
        {
            // Attach mouse wheel scrolling to the ScrollViewer
            DataListView.PreviewMouseWheel += (s, ev) =>
            {
                ScrollViewer scrollViewer = DataListView.Parent as ScrollViewer;
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - ev.Delta / 3);
                    ev.Handled = true;
                }
            };
        }




    }
}
