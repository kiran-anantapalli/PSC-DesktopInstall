using PSCInstaller.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PSCInstaller.Views
{
    public partial class StartPage : Page
    {
        public StartViewModel ViewModel
        {
            get { return DataContext as StartViewModel; }
        }

        public StartPage()
        {
            DataContext = new StartViewModel();
            InitializeComponent();
            this.Loaded += StartPage_Loaded;
            this.Unloaded += StartPage_Unloaded;      
        }

        void StartPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NavigateToInstall -= ViewModel_NavigateToInstall;
            ViewModel.NavigateToUnInstall -= ViewModel_NavigateToUninstall;
        }

        async void StartPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NavigateToInstall += ViewModel_NavigateToInstall;
            ViewModel.NavigateToUnInstall += ViewModel_NavigateToUninstall;
            await ViewModel.Initialize();
        }

        void ViewModel_NavigateToInstall(object sender, EventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService.Navigate(new Uri("Views/AppInstallationPage.xaml", UriKind.Relative));
        }

        void ViewModel_NavigateToUninstall(object sender, EventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService.Navigate(new Uri("Views/UnInstallPage.xaml", UriKind.Relative));
        }

    }
}
