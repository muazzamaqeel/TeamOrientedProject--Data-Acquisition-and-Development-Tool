using System.Windows;
using System.Windows.Controls;
using SmartPacifier.Interface.Services;

namespace Smart_Pacifier___Tool.Tabs.AlgorithmTab
{
    /// <summary>
    /// Interaction logic for AlgorithmsInternal.xaml
    /// </summary>
    public partial class AlgorithmsInternal : UserControl
    {
        private readonly string _campaignName;
        private readonly IDatabaseService _databaseService;

        public AlgorithmsInternal(string campaignName, IDatabaseService databaseService)
        {
            InitializeComponent();
            _campaignName = campaignName;
            _databaseService = databaseService;

            // Display a message box to confirm receipt
            MessageBox.Show($"Campaign Name: {_campaignName}", "AlgorithmsInternal");
        }
    }
}
