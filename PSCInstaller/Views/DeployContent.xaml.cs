using PSCInstaller.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PSCInstaller.Views
{
    public partial class DeployContent : Page
    {
        public DeployContentViewModel ViewModel
        {
            get { return DataContext as DeployContentViewModel; }
        }

        public DeployContent()
        {
            this.DataContext = new DeployContentViewModel();
            InitializeComponent();
            this.Loaded += DeployContent_Loaded; 
        }


        async void DeployContent_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.Initialize();
        }


    }
}
