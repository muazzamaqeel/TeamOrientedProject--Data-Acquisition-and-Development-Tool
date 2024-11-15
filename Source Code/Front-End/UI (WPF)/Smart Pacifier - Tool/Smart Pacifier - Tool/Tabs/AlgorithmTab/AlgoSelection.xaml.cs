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
            var liveDataView = new AlgoLiveData();
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.NavigateTo(liveDataView);
            }
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
