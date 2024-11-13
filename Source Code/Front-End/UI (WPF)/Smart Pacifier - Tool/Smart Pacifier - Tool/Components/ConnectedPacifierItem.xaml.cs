using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Smart_Pacifier___Tool.Components
{
    /// <summary>
    /// Interaction logic for ConnectedPacifierItem.xaml
    /// </summary>
    public partial class ConnectedPacifierItem : UserControl
    {
        public ConnectedPacifierItem()
        {
            InitializeComponent();
        }

        /// <summary>
        /// DependencyProperty for the ButtonText property.
        /// </summary>
        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(ConnectedPacifierItem), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the text of the button.
        /// </summary>
        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for the IsChecked property.
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(ConnectedPacifierItem), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the item is checked.
        /// </summary>
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        /// <summary>
        /// Event that is raised when the ToggleButton is toggled.
        /// </summary>
        public event RoutedEventHandler Toggled;

        /// <summary>
        /// Handles the ToggleButton's Checked and Unchecked events.
        /// </summary>
        private void TogglePacifierItem_Selected(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            IsChecked = toggleButton.IsChecked == true; // Update the IsChecked property

            // Raise the Toggled event
            Toggled?.Invoke(this, new RoutedEventArgs());
        }
    }
}