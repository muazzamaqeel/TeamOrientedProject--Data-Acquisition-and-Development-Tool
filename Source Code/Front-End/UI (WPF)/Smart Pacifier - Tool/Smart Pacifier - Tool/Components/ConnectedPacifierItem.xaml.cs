using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Smart_Pacifier___Tool.Components
{
    public partial class ConnectedPacifierItem : UserControl
    {
        public ConnectedPacifierItem()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(ConnectedPacifierItem), new PropertyMetadata(string.Empty));

        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(ConnectedPacifierItem), new PropertyMetadata(false));

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        // Custom event to notify when the ToggleButton is toggled
        public event RoutedEventHandler Toggled;

        private void TogglePacifierItem_Selected(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            IsChecked = toggleButton.IsChecked == true; // Update the IsChecked property

            // Raise the Toggled event
            Toggled?.Invoke(this, new RoutedEventArgs());
        }
    }
}
