/*
using System;
using System.Collections.Generic;
using System.Windows;
using SmartPacifier.Interface.Services;
using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using Smart_Pacifier___Tool.Components;
using InfluxDB.Client;


namespace Smart_Pacifier___Tool.Temp
{
    public partial class Test : Window
    {
        private readonly IManagerCampaign _managerCampaign;
        private readonly IManagerPacifiers _managerPacifiers;
        private readonly IManagerSensors _managerSensors;
        private readonly List<string> _campaigns = new List<string>();
        private readonly List<string> _pacifiers = new List<string>();

        public Test(IManagerCampaign managerCampaign, IManagerPacifiers managerPacifiers, IManagerSensors managerSensors)
        {
            InitializeComponent();
            _managerCampaign = managerCampaign;
            _managerPacifiers = managerPacifiers;
            _managerSensors = managerSensors;
            LoadExistingCampaigns();
        }

        public async void LoadExistingCampaigns()
        {
            _campaigns.Clear();
            var campaignsFromDb = await _managerCampaign.GetCampaignsAsync();

            if (campaignsFromDb != null && campaignsFromDb.Count > 0)
            {
                foreach (var campaign in campaignsFromDb)
                {
                    _campaigns.Add(campaign);
                }

                CampaignComboBox.ItemsSource = _campaigns;
            }
            else
            {
                MessageBox.Show("No campaigns found in the database.");
            }
        }

        private async void CampaignComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string? selectedCampaign = CampaignComboBox.SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(selectedCampaign))
            {
                await LoadPacifiers(selectedCampaign);
            }
        }

        public async Task LoadPacifiers(string selectedCampaign)
        {
            _pacifiers.Clear();
            var pacifiersFromDb = await _managerPacifiers.GetPacifiersAsync(selectedCampaign);

            if (pacifiersFromDb != null && pacifiersFromDb.Count > 0)
            {
                foreach (var pacifier in pacifiersFromDb)
                {
                    _pacifiers.Add(pacifier);
                }

                PacifierComboBox.ItemsSource = _pacifiers;
            }
            else
            {
                MessageBox.Show("No pacifiers found for the selected campaign.");
            }
        }

        private async void OnAddCampaignButtonClick(object sender, RoutedEventArgs e)
        {
            InputDialog inputDialog = new InputDialog("Enter Campaign Name");
            if (inputDialog.ShowDialog() == true)
            {
                string newCampaignName = inputDialog.InputText;

                if (!string.IsNullOrWhiteSpace(newCampaignName))
                {
                    await _managerCampaign.AddCampaignAsync(newCampaignName);
                    _campaigns.Add(newCampaignName);
                    CampaignComboBox.ItemsSource = _campaigns;
                    ResultsTextBox.Text += $"Added campaign: {newCampaignName}\n";
                }
                else
                {
                    MessageBox.Show("Please enter a valid campaign name.");
                }
            }
        }

        private async void OnAddPacifierButtonClick(object sender, RoutedEventArgs e)
        {
            string? selectedCampaign = CampaignComboBox.SelectedItem as string;

            if (selectedCampaign != null)
            {
                InputDialog inputDialog = new InputDialog("Enter Pacifier Name");
                if (inputDialog.ShowDialog() == true)
                {
                    string pacifierName = inputDialog.InputText;

                    if (!string.IsNullOrWhiteSpace(pacifierName))
                    {
                        await _managerPacifiers.AddPacifierAsync(selectedCampaign, pacifierName);
                        ResultsTextBox.Text += $"Added pacifier: {pacifierName} to campaign: {selectedCampaign}\n";
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a campaign.");
            }
        }

        private async void OnAddSensorButtonClick(object sender, RoutedEventArgs e)
        {
            string? selectedCampaign = CampaignComboBox.SelectedItem as string;
            string? selectedPacifier = PacifierComboBox.SelectedItem as string;

            if (selectedCampaign != null && selectedPacifier != null)
            {
                float ppgValue = 0.85f;
                float imuAccelX = 0.001f;
                float imuAccelY = 0.002f;
                float imuAccelZ = 0.003f;

                await _managerSensors.AddSensorDataAsync(selectedPacifier, ppgValue, imuAccelX, imuAccelY, imuAccelZ);
                ResultsTextBox.Text += $"Added sensors to pacifier: {selectedPacifier} in campaign: {selectedCampaign}\n";
            }
            else
            {
                MessageBox.Show("Please select a campaign and pacifier.");
            }
        }
    }
}
*/