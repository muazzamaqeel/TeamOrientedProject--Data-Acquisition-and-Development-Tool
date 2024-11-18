using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Components
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : Window
    {
        private CancellationTokenSource _cancellationTokenSource;

        public ProgressDialog()
        {
            InitializeComponent();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public void UpdateProgress(int progress, string message)
        {
            ProgressBar.Value = progress;
            ProgressMessage.Text = message;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();
            Close();
        }
    }
}
