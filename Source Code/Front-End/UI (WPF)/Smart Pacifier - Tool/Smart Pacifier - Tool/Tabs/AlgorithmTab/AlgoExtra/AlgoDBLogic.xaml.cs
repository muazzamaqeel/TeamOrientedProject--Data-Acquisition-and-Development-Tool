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

        private void LoadAvailableScripts()
        {
            try
            {
                string scriptsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "OutputResources", "PythonFiles", "ExecutableScript");

                if (Directory.Exists(scriptsDirectory))
                {
                    var scriptFiles = Directory.GetFiles(scriptsDirectory, "*.py");
                    foreach (var script in scriptFiles)
                    {
                        PythonScripts.Add(Path.GetFileName(script));
                    }

                    if (PythonScripts.Count > 0)
                    {
                        SelectedScript = PythonScripts[0];
                    }

                    AppendMessage("Python scripts loaded successfully.");
                }
                else
                {
                    AppendMessage($"Error: Scripts directory not found at {scriptsDirectory}");
                }
            }
            catch (Exception ex)
            {
                AppendMessage($"Error loading scripts: {ex.Message}");
            }
        }

        private async void RunScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedScript))
                {
                    AppendMessage("No script selected. Please select a script.");
                    return;
                }

                var campaignData = await _databaseService.GetCampaignDataAlgorithmLayerAsync(_campaignName);

                if (campaignData == null || campaignData.Count == 0)
                {
                    AppendMessage($"No data found for campaign {_campaignName}.");
                    return;
                }

                string campaignDataJson = JsonSerializer.Serialize(new
                {
                    CampaignName = _campaignName,
                    Pacifiers = campaignData
                });

                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "OutputResources", "PythonFiles", "ExecutableScript", SelectedScript);

                if (!File.Exists(scriptPath))
                {
                    AppendMessage($"Script file not found: {scriptPath}");
                    return;
                }

                var pythonScriptEngine = new PythonScriptEngine();
                string result = await pythonScriptEngine.ExecuteScriptAsync(scriptPath, campaignDataJson);

                AppendMessage($"Script executed successfully:\n{result}");
            }
            catch (Exception ex)
            {
                AppendMessage($"Error running script: {ex.Message}");
            }
        }

        private void AppendMessage(string message)
        {
            if (Dispatcher.CheckAccess())
            {
                ScriptOutput += $"{DateTime.Now}: {message}\n";
            }
            else
            {
                Dispatcher.Invoke(() => ScriptOutput += $"{DateTime.Now}: {message}\n");
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private async void RunScriptWithDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedScript))
                {
                    AppendMessage("No script selected. Please select a script.");
                    return;
                }

                // Fetch data from the database
                var campaignData = await _databaseService.GetCampaignDataAlgorithmLayerAsync(_campaignName);

                if (campaignData == null || campaignData.Count == 0)
                {
                    AppendMessage($"No data found for campaign {_campaignName}.");
                    return;
                }

                // Serialize campaign data to JSON
                string campaignDataJson = JsonSerializer.Serialize(new
                {
                    CampaignName = _campaignName,
                    Pacifiers = campaignData
                });

                // Construct the script path
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "OutputResources", "PythonFiles", "ExecutableScript", SelectedScript);

                if (!File.Exists(scriptPath))
                {
                    AppendMessage($"Script file not found: {scriptPath}");
                    return;
                }

                // Use the Python script engine to execute the script
                var pythonScriptEngine = new PythonScriptEngine();
                string result = await pythonScriptEngine.ExecuteScriptAsync(scriptPath, campaignDataJson);

                // Display the result in the UI
                AppendMessage($"Script executed successfully:\n{result}");
            }
            catch (Exception ex)
            {
                AppendMessage($"Error running script: {ex.Message}");
            }
        }



    }
}
