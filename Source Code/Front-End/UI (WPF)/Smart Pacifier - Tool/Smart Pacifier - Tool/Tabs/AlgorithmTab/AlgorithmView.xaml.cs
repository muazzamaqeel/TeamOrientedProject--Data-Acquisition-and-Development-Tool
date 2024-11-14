using System.Windows;
using System.Windows.Controls;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.AlgorithmTab
{
    public partial class AlgorithmView : UserControl
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAlgorithmLayer _algorithmLayer;

        public AlgorithmView(IDatabaseService databaseService, IAlgorithmLayer algorithmLayer)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _algorithmLayer = algorithmLayer;
        }

        private void RunAlgorithmButton_Click(object sender, RoutedEventArgs e)
        {
            var config = ((App)Application.Current).LoadDatabaseConfiguration();
            string scriptName = config.PythonScript?.FileName ?? "python1.py"; // Default to "python1.py" if not specified

            try
            {
                string result = _algorithmLayer.ExecuteScript(scriptName);
                MessageBox.Show($"Python script '{scriptName}' executed successfully.", "Execution Success", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing Python script:\n{ex.Message}", "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
