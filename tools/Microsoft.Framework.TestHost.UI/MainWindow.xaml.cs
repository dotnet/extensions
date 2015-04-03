
using System.Windows;

namespace Microsoft.Framework.TestHost.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel(this);
            DataContext.DNX = DNX.FindDnx();
        }

        public new MainWindowViewModel DataContext
        {
            get
            {
                return (MainWindowViewModel)base.DataContext;
            }
            set
            {
                base.DataContext = value;
            }
        }
    }
}
