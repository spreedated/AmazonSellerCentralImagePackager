using AmazonSellerCentralImagePackager.ViewModels;
using Avalonia.Controls;

namespace AmazonSellerCentralImagePackager.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _vm;

        public MainWindow()
        {
            this.InitializeComponent();

            _vm = new MainWindowViewModel
            {
                Instance = this
            };
            this.DataContext = _vm;
        }
    }
}