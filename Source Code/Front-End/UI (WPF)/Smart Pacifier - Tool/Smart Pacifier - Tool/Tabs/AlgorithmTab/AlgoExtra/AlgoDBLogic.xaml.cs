using SmartPacifier.Interface.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Tabs.AlgorithmTab.AlgoExtra
{
    public partial class AlgoDBLogic : UserControl, INotifyPropertyChanged
    {
        private readonly string _campaignName;
        private readonly IDatabaseService _databaseService;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public AlgoDBLogic(string campaignName, IDatabaseService databaseService)
        {
            InitializeComponent();
            _campaignName = campaignName;
            _databaseService = databaseService;

            DataContext = this;
            LoadAvailableScripts();
        }

        /// <summary>
        /// Loads available Python scripts from the specified directory into the ComboBox.
        /// </summary>
        private void LoadAvailableScripts()
        {
            try
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
                    AppendMessage("Available Python scripts loaded.");
                }
                else
                {
                    string errorMsg = $"Scripts directory not found at: {scriptsDirectory}";
                    Debug.WriteLine(errorMsg);
                    AppendMessage($"Error: {errorMsg}");
                }

                if (PythonScripts.Count > 0)
                {
                    SelectedScript = PythonScripts[0];
                    AppendMessage($"Selected script: {SelectedScript}");
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error loading scripts: {ex.Message}";
                Debug.WriteLine(errorMsg);
                AppendMessage($"Error: {errorMsg}");
            }
        }

        /// <summary>
        /// Handles the Run Script button click event.
        /// </summary>
        private async void RunScriptButton_Click(object sender, RoutedEventArgs e)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "algorithm_debug_log.txt");

            try
            {
                await LogAsync(logPath, "RunScriptButton_Click started");

                if (string.IsNullOrEmpty(SelectedScript))
                {
                    await LogAsync(logPath, "No script selected");
                    AppendMessage("Please select a script to run.");
                    return;
                }

                await LogAsync(logPath, $"Selected Script: {SelectedScript}");

                // Retrieve the campaign data from the database
                var campaignData = await _databaseService.GetCampaignDataAlgorithmLayerAsync(_campaignName);
                await LogAsync(logPath, "Campaign data retrieved from database");

                if (campaignData == null || campaignData.Count == 0)
                {
                    await LogAsync(logPath, "No data found for the selected campaign");
                    AppendMessage("No data found for the selected campaign.");
                    return;
                }

                // Convert campaign data to JSON format
                var campaignDataJson = JsonSerializer.Serialize(new
                {
                    CampaignName = _campaignName,
                    Pacifiers = campaignData
                });
                await LogAsync(logPath, "Campaign data converted to JSON format");

                // Construct the full path to the Python script
                var baseDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
                var scriptsDirectoryPath = Path.Combine(baseDirectoryPath, @"..\..\..\Resources\OutputResources\PythonFiles\ExecutableScript");
                var scriptPath = Path.Combine(scriptsDirectoryPath, SelectedScript);
                scriptPath = Path.GetFullPath(scriptPath); // Ensure full path

                if (!File.Exists(scriptPath))
                {
                    string errorMsg = $"Script file not found at path: {scriptPath}";
                    await LogAsync(logPath, errorMsg);
                    AppendMessage($"Error: {errorMsg}");
                    return;
                }

                await LogAsync(logPath, $"Script path confirmed: {scriptPath}");

                // Create a new instance of PythonScriptEngine
                var pythonScriptEngine = new PythonScriptEngine();

                // Execute the Python script, passing the JSON data as an argument
                string result = await pythonScriptEngine.ExecuteScriptAsync(scriptPath, campaignDataJson);
                await LogAsync(logPath, $"Python script executed, result: {result}");

                // Update the UI with the result
                AppendMessage($"Script Output:\n{result}");
            }
            catch (TimeoutException tex)
            {
                string timeoutMsg = $"Script execution timed out: {tex.Message}";
                Debug.WriteLine(timeoutMsg);
                AppendMessage($"Error: {timeoutMsg}");
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error executing Python script: {ex.Message}";
                await LogAsync(logPath, $"{errorMsg}\nDetails:\n{ex.StackTrace}");
                Debug.WriteLine(errorMsg);
                AppendMessage($"Error: {errorMsg}");
            }
        }

        /// <summary>
        /// Sends data to the Python script and retrieves the response.
        /// </summary>
        /// <param name="scriptPath">Full path to the Python script.</param>
        /// <param name="campaignDataJson">JSON-formatted campaign data.</param>
        /// <returns>Response from the Python script.</returns>
        private async Task<string> SendDataToPythonScriptAsync(string scriptPath, string campaignDataJson)
        {
            var pythonScriptEngine = new PythonScriptEngine();
            string response = await pythonScriptEngine.ExecuteScriptAsync(scriptPath, campaignDataJson);
            return response;
        }

        /// <summary>
        /// Logs messages to a specified log file asynchronously.
        /// </summary>
        /// <param name="logPath">Path to the log file.</param>
        /// <param name="message">Message to log.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task LogAsync(string logPath, string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logPath, append: true))
                {
                    string logMessage = $"{DateTime.Now}: {message}";
                    await writer.WriteLineAsync(logMessage);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error writing to log file: {ex.Message}";
                Debug.WriteLine(errorMsg);
                AppendMessage($"Logging Error: {errorMsg}");
            }
        }

        /// <summary>
        /// Appends a message to the ScriptOutput TextBox in a thread-safe manner.
        /// </summary>
        /// <param name="message">The message to append.</param>
        private void AppendMessage(string message)
        {
            // Ensure UI updates are performed on the UI thread
            if (Dispatcher.CheckAccess())
            {
                ScriptOutput += $"{DateTime.Now}: {message}\n";
            }
            else
            {
                Dispatcher.Invoke(() => ScriptOutput += $"{DateTime.Now}: {message}\n");
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event to notify the UI about property updates.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
