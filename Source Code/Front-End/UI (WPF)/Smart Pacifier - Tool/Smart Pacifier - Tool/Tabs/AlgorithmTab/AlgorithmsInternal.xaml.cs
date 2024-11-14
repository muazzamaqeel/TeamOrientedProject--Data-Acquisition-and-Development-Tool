using System;
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
        private readonly IAlgorithmLayer _algorithmLayer;

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

        public AlgorithmsInternal(string campaignName, IDatabaseService databaseService, IAlgorithmLayer algorithmLayer)
        {
            InitializeComponent();
            _campaignName = campaignName;
            _databaseService = databaseService;
            _algorithmLayer = algorithmLayer;

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

        private void RunScriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedScript))
            {
                MessageBox.Show("Please select a script to run.", "No Script Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Execute the script, passing the campaign name as an argument
                string result = _algorithmLayer.ExecuteScript(SelectedScript, _campaignName);
                ScriptOutput = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing Python script:\n{ex.Message}", "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
