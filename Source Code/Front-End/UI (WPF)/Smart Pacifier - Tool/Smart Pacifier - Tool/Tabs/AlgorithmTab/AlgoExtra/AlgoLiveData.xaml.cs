using SmartPacifier.Interface.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using SmartPacifier.BackEnd.CommunicationLayer.MQTT;
using System.Threading.Tasks;

namespace Smart_Pacifier___Tool.Tabs.AlgorithmTab.AlgoExtra
{
    public partial class AlgoLiveData : UserControl, INotifyPropertyChanged
    {
        private readonly string _campaignName;
        private readonly IDatabaseService _databaseService;
        private readonly PythonScriptEngine _pythonScriptEngine;

        public event PropertyChangedEventHandler PropertyChanged;

        // Properties for live data source selection and output
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

        private string _liveDataOutput;
        public string LiveDataOutput
        {
            get => _liveDataOutput;
            set
            {
                _liveDataOutput = value;
                OnPropertyChanged(nameof(LiveDataOutput));
            }
        }

        public AlgoLiveData(string campaignName, IDatabaseService databaseService)
        {
            InitializeComponent();
            _campaignName = campaignName;
            _databaseService = databaseService;
            _pythonScriptEngine = new PythonScriptEngine();

            DataContext = this;
            LoadAvailableScripts();
            SubscribeToBroker();
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

        // Subscribe to the broker to receive real-time data
        private void SubscribeToBroker()
        {
            Broker.Instance.MessageReceived += OnMessageReceived;
            Debug.WriteLine("AlgoLiveData: Subscribed to broker messages.");
        }

        // Handle incoming broker messages and send data to Python for processing
        private async void OnMessageReceived(object? sender, Broker.MessageReceivedEventArgs e)
        {
            // Convert parsed data to JSON format
            var liveDataJson = JsonSerializer.Serialize(new
            {
                PacifierId = e.PacifierId,
                SensorType = e.SensorType,
                Data = e.ParsedData
            });

            // Update the LiveDataOutput property to display the live data
            LiveDataOutput = $"Received Data:\n{liveDataJson}";

            // Send data to the Python script for additional processing
            await SendDataToPythonScript(liveDataJson);
        }

        private async Task SendDataToPythonScript(string liveDataJson)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "live_data_script_log.txt");

            try
            {
                File.AppendAllText(logPath, "Sending live data to Python script\n");

                // Define the Python script path
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var scriptPath = Path.Combine(baseDirectory, @"Resources\OutputResources\PythonFiles\ExecutableScript", SelectedScript);

                // Execute the Python script, passing the live data JSON
                string result = await _pythonScriptEngine.ExecuteScriptWithTcpAsync(scriptPath, liveDataJson);
                File.AppendAllText(logPath, $"Python script executed, result: {result}\n");

                // Update the UI with the Python script output if needed
                LiveDataOutput += $"\n\nProcessed Output:\n{result}";
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error executing Python script with live data: {ex.Message}";
                File.AppendAllText(logPath, errorMsg + "\nDetails:\n" + ex.StackTrace);
                MessageBox.Show(errorMsg, "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Start monitoring live data
        private void StartMonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedScript))
            {
                MessageBox.Show("Please select a script to run.", "No Script Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LiveDataOutput = $"Monitoring live data using script: {SelectedScript}";
        }

        // Raise the PropertyChanged event to notify the UI about property updates
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
