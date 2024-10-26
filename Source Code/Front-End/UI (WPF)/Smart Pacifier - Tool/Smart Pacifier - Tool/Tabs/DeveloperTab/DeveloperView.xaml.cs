using SmartPacifier.Interface.Services;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace Smart_Pacifier___Tool.Tabs.DeveloperTab
{
    public partial class DeveloperView : UserControl
    {
        private readonly IDatabaseService _databaseService;
        private readonly IManagerPacifiers _managerPacifiers;
        private readonly IManagerCampaign _managerCampaign; 

        private List<SensorData> allData = new List<SensorData>();
        private List<SensorData> currentPageData = new List<SensorData>();
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
                var campaigns = await _databaseService.GetCampaignsAsync();
                Campaign.ItemsSource = campaigns;

                allData.Clear();
                foreach (var campaign in campaigns)
                {
                    var pacifiers = await _managerPacifiers.GetPacifiersAsync(campaign); // Fetch actual pacifiers for each campaign

                    foreach (var pacifier in pacifiers)
                    {
                        allData.Add(new SensorData
                        {
                            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            Campaign = campaign,
                            Pacifier = pacifier, // Use the actual pacifier from data source
                            Sensor = "sensor_1", // Replace with actual sensor data if available
                            Value = 36.5 // Replace with actual sensor values if available
                        });
                    }
                }

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

            _managerCampaign.EndCampaignAsync("Campaign 5");
            //AddDataWindow addDataWindow = new AddDataWindow();
            //addDataWindow.ShowDialog();
        }


        // Edit Button Click Event Handler
        // Edit Button Click Event Handler
        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = allData.Where(item => item.IsSelected).ToList();
            if (selectedItems.Count != 1)
            {
                MessageBox.Show("Please select exactly one item to edit.");
                return;
            }

            var editDataWindow = new EditDataWindow(selectedItems.First(), _managerCampaign, _managerPacifiers);
            bool? result = editDataWindow.ShowDialog();

            // Check if the dialog result was OK
            if (result == true)
            {
                await LoadDataAsync(); // Reload the data if Save was successful
            }
        }




        private async void OpenEditDataWindow(SensorData data)
        {
            var editDataWindow = new EditDataWindow(data, _managerCampaign, _managerPacifiers);
            if (editDataWindow.ShowDialog() == true)
            {
                await LoadDataAsync(); // Refresh data after edit
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



public class SensorData : INotifyPropertyChanged
    {
        private string _timestamp;
        private string _campaign;
        private string _pacifier;
        private string _sensor;
        private double _value;
        private bool _isSelected;

        public string Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(nameof(Timestamp)); }
        }

        public string Campaign
        {
            get => _campaign;
            set { _campaign = value; OnPropertyChanged(nameof(Campaign)); }
        }

        public string Pacifier
        {
            get => _pacifier;
            set { _pacifier = value; OnPropertyChanged(nameof(Pacifier)); }
        }

        public string Sensor
        {
            get => _sensor;
            set { _sensor = value; OnPropertyChanged(nameof(Sensor)); }
        }

        public double Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(nameof(Value)); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


}
