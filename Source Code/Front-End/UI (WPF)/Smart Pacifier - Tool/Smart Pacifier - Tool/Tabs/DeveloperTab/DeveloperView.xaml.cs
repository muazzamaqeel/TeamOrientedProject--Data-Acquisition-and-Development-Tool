using SmartPacifier.Interface.Services;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Data;

namespace Smart_Pacifier___Tool.Tabs.DeveloperTab
{
    public partial class DeveloperView : UserControl
    {
        private readonly IDatabaseService _databaseService;
        private readonly IManagerPacifiers _managerPacifiers;
        private readonly IManagerCampaign _managerCampaign;

        private DataTable allData = new DataTable();
        private int currentPage = 1;
        private int pageSize = 10;

        public DeveloperView(IDatabaseService databaseService, IManagerCampaign managerCampaign, IManagerPacifiers managerPacifiers)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _managerPacifiers = managerPacifiers;
            _managerCampaign = managerCampaign;

            _ = LoadDataAsync();  // Call the async method but don't await, suppressing CS4014
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Load campaigns
                var campaigns = await _databaseService.GetCampaignsAsync();
                Campaign.ItemsSource = campaigns;

                // Load pacifiers for the first selected campaign if any
                if (campaigns.Any())
                {
                    var pacifiers = await _managerPacifiers.GetPacifiersAsync(campaigns.First());
                    Pacifier.ItemsSource = pacifiers;
                }

                // Load all sensor data from the database
                allData.Clear();
                allData = await _databaseService.GetSensorDataAsync(); // Ensure that this returns data correctly

                DisplayData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }


        private void DisplayData()
        {
            if (allData.Rows.Count == 0)
            {
                MessageBox.Show("No data to display.");
                return;
            }

            DataTable paginatedData = allData.Clone();
            int startIndex = (currentPage - 1) * pageSize;
            int endIndex = Math.Min(startIndex + pageSize, allData.Rows.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                paginatedData.ImportRow(allData.Rows[i]);
            }

            DataListView.ItemsSource = paginatedData.DefaultView;
            MessageBox.Show($"Displaying {paginatedData.Rows.Count} rows.", "DisplayData");
        }



        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedCampaign = Campaign.SelectedItem?.ToString();
            string selectedPacifier = Pacifier.SelectedItem?.ToString();
            string selectedSensorType = Sensor.SelectedItem?.ToString();

            var filteredData = allData.AsEnumerable().Where(row =>
                (string.IsNullOrEmpty(selectedCampaign) || row["campaign_name"].ToString() == selectedCampaign) &&
                (string.IsNullOrEmpty(selectedPacifier) || row["pacifier_name"].ToString() == selectedPacifier) &&
                (string.IsNullOrEmpty(selectedSensorType) || row["sensor_type"].ToString() == selectedSensorType));

            DataTable filteredTable = filteredData.CopyToDataTable();
            DataListView.ItemsSource = filteredTable.DefaultView;
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                DisplayData();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage * pageSize < allData.Rows.Count)
            {
                currentPage++;
                DisplayData();
            }
        }

        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == Campaign)
            {
                var selectedCampaign = Campaign.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedCampaign))
                {
                    var pacifiers = await _managerPacifiers.GetPacifiersAsync(selectedCampaign);
                    Pacifier.ItemsSource = pacifiers;

                    // Filter and display data for the selected campaign
                    var filteredData = allData.AsEnumerable().Where(row =>
                        row["campaign_name"].ToString() == selectedCampaign);
                    DataListView.ItemsSource = filteredData.CopyToDataTable().DefaultView;
                }
            }
        }


        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _managerCampaign.EndCampaignAsync("Campaign 10");
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Edit functionality not implemented for direct database rows.");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Delete functionality not implemented for direct database rows.");
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
