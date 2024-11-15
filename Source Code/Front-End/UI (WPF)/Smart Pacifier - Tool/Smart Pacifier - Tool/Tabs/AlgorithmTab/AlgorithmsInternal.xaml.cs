using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.AlgorithmTab
{
    public partial class AlgorithmsInternal : UserControl, INotifyPropertyChanged
    {
        private readonly string _campaignName;
        private readonly IDatabaseService _databaseService;
        private readonly PythonScriptEngine _pythonScriptEngine;
        public event PropertyChangedEventHandler PropertyChanged;

        // Properties for script selection and output
        public ObservableCollection<string> PythonScripts { get; set; } = new ObservableCollection<string>();

        private string _selectedScript;
        public string SelectedScript
        {
            get => _selectedScript;
            set
            {
                _selectedScript = value;
                OnPropertyChanged(nameof(SelectedScript));
            }
        }

        private string _scriptOutput;
        public string ScriptOutput
        {
            get => _scriptOutput;
            set
            {
                _scriptOutput = value;
                OnPropertyChanged(nameof(ScriptOutput));
            }
        }

        public string CampaignName => _campaignName;

        public AlgorithmsInternal(string campaignName, IDatabaseService databaseService)
        {
            InitializeComponent();
            _campaignName = campaignName;
            _databaseService = databaseService;

            // Instantiate PythonScriptEngine directly
            _pythonScriptEngine = new PythonScriptEngine();

            DataContext = this;

            LoadAvailableScripts();
        }

        // Load available Python scripts into the ComboBox
        private void LoadAvailableScripts()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string scriptsDirectory = Path.Combine(baseDirectory, @"..\..\..\Resources\OutputResources\PythonFiles\ExecutableScript");
            scriptsDirectory = Path.GetFullPath(scriptsDirectory);

            if (Directory.Exists(scriptsDirectory))
            {
                var scriptFiles = Directory.GetFiles(scriptsDirectory, "*.py");
                foreach (var script in scriptFiles)
                {
                    PythonScripts.Add(Path.GetFileName(script));
                }
            }
            else
            {
                MessageBox.Show($"Scripts directory not found at: {scriptsDirectory}", "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (PythonScripts.Count > 0)
            {
                SelectedScript = PythonScripts[0];
            }
        }

        private async void RunScriptButton_Click(object sender, RoutedEventArgs e)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "algorithm_debug_log.txt");

            try
            {
                File.AppendAllText(logPath, "RunScriptButton_Click started\n");

                if (string.IsNullOrEmpty(SelectedScript))
                {
                    File.AppendAllText(logPath, "No script selected\n");
                    MessageBox.Show("Please select a script to run.", "No Script Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                File.AppendAllText(logPath, $"Selected Script: {SelectedScript}\n");

                // Retrieve the campaign data from the database
                var campaignData = await _databaseService.GetCampaignDataAlgorithmLayerAsync(_campaignName);
                File.AppendAllText(logPath, "Campaign data retrieved from database\n");

                if (campaignData == null || campaignData.Count == 0)
                {
                    File.AppendAllText(logPath, "No data found for the selected campaign\n");
                    MessageBox.Show("No data found for the selected campaign.", "Data Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Convert campaign data to JSON format
                var campaignDataJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    CampaignName = _campaignName,
                    Pacifiers = campaignData
                });
                File.AppendAllText(logPath, "Campaign data converted to JSON format\n");

                // Construct the full path to the Python script
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var scriptsDirectory = Path.Combine(baseDirectory, @"..\..\..\Resources\OutputResources\PythonFiles\ExecutableScript");
                var scriptPath = Path.Combine(scriptsDirectory, SelectedScript);

                if (!File.Exists(scriptPath))
                {
                    string errorMsg = $"Script file not found at path: {scriptPath}";
                    File.AppendAllText(logPath, errorMsg + "\n");
                    MessageBox.Show(errorMsg, "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                File.AppendAllText(logPath, $"Script path confirmed: {scriptPath}\n");
                MessageBox.Show($"Script path confirmed: {scriptPath}", "Path Confirmed", MessageBoxButton.OK, MessageBoxImage.Information);

                // Execute the Python script, passing the JSON data as an argument
                string result = await _pythonScriptEngine.ExecuteScriptAsync(scriptPath, campaignDataJson);
                File.AppendAllText(logPath, $"Python script executed, result: {result}\n");

                // Save result to a text file
                string resultFilePath = Path.Combine(baseDirectory, "PythonScriptResult.txt");
                File.WriteAllText(resultFilePath, result);
                File.AppendAllText(logPath, $"Result saved to {resultFilePath}\n");

                // Update the UI with the result
                ScriptOutput = result;
                MessageBox.Show("Python script execution completed. Check output for details.", "Execution Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error executing Python script: {ex.Message}";
                File.AppendAllText(logPath, errorMsg + "\nDetails:\n" + ex.StackTrace);
                MessageBox.Show(errorMsg, "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Raise the PropertyChanged event to notify the UI about property updates
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
