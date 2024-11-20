using Microsoft.Extensions.DependencyInjection;
using Smart_Pacifier___Tool.Tabs.AlgorithmTab.AlgoExtra;
using SmartPacifier.Interface.Services;
using System.Windows;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Tabs.AlgorithmTab
{
    public partial class AlgoSelection : UserControl
    {
        public AlgoSelection()
        {
            InitializeComponent();
        }

        private void LiveDataButton_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve necessary services
            var databaseService = ((App)Application.Current).ServiceProvider.GetRequiredService<IDatabaseService>();
            string campaignName = "Live Monitoring Campaign"; // Use a default or specific campaign name

            // Create an instance of AlgoLiveData with the required parameters
            var algoLiveData = new AlgoLiveData(campaignName, databaseService);

            // Navigate to the new view
            ((MainWindow)Application.Current.MainWindow).NavigateTo(algoLiveData);
        }


        private void DatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            var databaseService = ((App)Application.Current).ServiceProvider.GetRequiredService<IDatabaseService>();
            var managerCampaign = ((App)Application.Current).ServiceProvider.GetRequiredService<IManagerCampaign>();

            var algorithmView = new AlgorithmView(databaseService, managerCampaign);

            // Navigate to AlgorithmView in the main window
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.NavigateTo(algorithmView);
            }
        }

    }
}
