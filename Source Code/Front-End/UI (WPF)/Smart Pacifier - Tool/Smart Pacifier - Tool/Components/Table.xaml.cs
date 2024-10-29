using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Components
{
    public partial class TableUserControl : UserControl
    {
        public TableUserControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(TableUserControl), new PropertyMetadata(null));

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
    }
}