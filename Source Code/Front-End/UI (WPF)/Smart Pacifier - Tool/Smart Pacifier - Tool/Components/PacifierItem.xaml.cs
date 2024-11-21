using OxyPlot.Series;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Smart_Pacifier___Tool.Components
{
    public partial class PacifierItem : UserControl, INotifyPropertyChanged
    {
        public event EventHandler? ToggleChanged;

        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(PacifierItem), new PropertyMetadata("basePacifier"));

        public static readonly DependencyProperty CircleTextProperty =
            DependencyProperty.Register("CircleText", typeof(string), typeof(PacifierItem), new PropertyMetadata("N/A"));

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(PacifierItem), new PropertyMetadata(false));


        public string PacifierId
        {
            get;
            set;
        }

        public bool HasRow
        {
            get;
            set;
        }

        public int UpdateFrequency
        {
            get;
            set;
        }

        private string _status;

        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));  // Notify UI of the property change
                }
            }
        }

        public ObservableCollection<byte[]> RawData { get; private set; } = new ObservableCollection<byte[]>();

        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }

        public string CircleText
        {
            get { return (string)GetValue(CircleTextProperty); }
            set { SetValue(CircleTextProperty, value); }
        }

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        // Observable collection for sensor data
        public ObservableCollection<SensorItem> Sensors 
        {
            get; 
            set; 
        }

        public List<object> CampaignData { get; set; }

        public PacifierItem(string pacifierId)
        {
            InitializeComponent();

            IsChecked = false;
            PacifierId = pacifierId;
            ButtonText = "basePacifier";
            HasRow = false;
            UpdateFrequency = 500;
            Status = "Connected";

            Sensors = new ObservableCollection<SensorItem>();
            CampaignData = new List<object>();


            DataContext = this;
        }

        public PacifierItem GetPacifierItem()
        {
            return this;
        }

        public bool HasSensor(SensorItem sensorItem)
        {
            return Sensors.Contains(sensorItem); // Where Sensors is a collection in PacifierItem
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                ToggleChanged?.Invoke(this, EventArgs.Empty);
                IsChecked = toggleButton.IsChecked == true; // Update the IsChecked property
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
