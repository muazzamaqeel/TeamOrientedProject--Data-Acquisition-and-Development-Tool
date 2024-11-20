using System;
using System.Windows;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Temp
{
    public partial class AlgoTest : Window
    {
        private readonly IAlgorithmLayer _algorithmService;

        public AlgoTest(IAlgorithmLayer algorithmService)
        {
            InitializeComponent();
            _algorithmService = algorithmService;
        }

        private void RunPythonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pythonCode = PythonCodeInput.Text;
                //string output = _algorithmService.ExecuteScript(pythonCode);
                //OutputTextBox.Text = output;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running Python code: {ex.Message}");
            }
        }
    }
}
