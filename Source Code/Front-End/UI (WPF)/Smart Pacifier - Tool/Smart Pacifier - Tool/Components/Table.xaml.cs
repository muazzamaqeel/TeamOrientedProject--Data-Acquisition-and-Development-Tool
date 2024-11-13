using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Smart_Pacifier___Tool.Components
{
    /// <summary>
    /// Interaction logic for TableUserControl.xaml
    /// </summary>
    public partial class TableUserControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableUserControl"/> class.
        /// </summary>
        public TableUserControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// DependencyProperty to bind an IEnumerable as the ItemsSource.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(TableUserControl), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the ItemsSource for the table.
        /// </summary>
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
    }
}