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
    public partial class AppInstallationPage : Page
    {
        public AppInstallationViewModel ViewModel
        {
            get { return DataContext as AppInstallationViewModel; }
        }

        public AppInstallationPage()
        {
            DataContext = new AppInstallationViewModel();
            InitializeComponent();
            this.Loaded += AppInstallationPage_Loaded;
            this.Unloaded += AppInstallationPage_Unloaded;
        }

        void AppInstallationPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NavigateToStart -= ViewModel_NavigateToStart;
            ViewModel.NavigateToContentInstall -= ViewModel_NavigateToContentInstall;
            ViewModel.Dispose();
        }

        async void AppInstallationPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NavigateToStart += ViewModel_NavigateToStart;
            ViewModel.NavigateToContentInstall += ViewModel_NavigateToContentInstall;
            await ViewModel.Initialize();
        }

        void ViewModel_NavigateToContentInstall(object sender, EventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService.Navigate(new Uri("Views/ContentInstallationPage.xaml", UriKind.Relative));  
        }

        void ViewModel_NavigateToStart(object sender, EventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService.Navigate(new Uri("Views/StartPage.xaml", UriKind.Relative));
        }
    }
}
