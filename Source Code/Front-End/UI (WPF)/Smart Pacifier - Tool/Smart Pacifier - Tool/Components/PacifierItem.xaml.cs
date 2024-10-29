using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Smart_Pacifier___Tool.Components
{
    public partial class PacifierItem : UserControl
    {
        public event EventHandler ToggleChanged;

        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(PacifierItem), new PropertyMetadata("Pacifier"));

        public static readonly DependencyProperty CircleTextProperty =
            DependencyProperty.Register("CircleText", typeof(string), typeof(PacifierItem), new PropertyMetadata("1"));

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(PacifierItem), new PropertyMetadata(false));


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

        public PacifierItem()
        {
            InitializeComponent();
            DataContext = this;
        }

        public Grid AssociatedGrid 
        {
            get;
            set;
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            if (toggleButton != null)
            {
                ToggleChanged?.Invoke(this, EventArgs.Empty);
            }
        }

    }
}
