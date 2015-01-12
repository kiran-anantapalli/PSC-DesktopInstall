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
    /// <summary>
    /// Interaction logic for PackageSelectionPage.xaml
    /// </summary>
    public partial class PackageSelectionPage : Page
    {
        public ContentPackageSelectionViewModel ViewModel
        {
            get { return DataContext as ContentPackageSelectionViewModel; }
        }

        public PackageSelectionPage()
        {
            DataContext = new ContentPackageSelectionViewModel();
            InitializeComponent();
            this.Loaded += PackageSelectionPage_Loaded;
            this.Unloaded += PackageSelectionPage_Unloaded;
        }

        void PackageSelectionPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NavigateToApplicationInstallation -= ViewModel_NavigateToApplicationInstallation;
            ViewModel.NavigateToContentInstall -= ViewModel_NavigateToContentInstall;
            ViewModel.Dispose();
        }

        async void PackageSelectionPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NavigateToApplicationInstallation += ViewModel_NavigateToApplicationInstallation;
            ViewModel.NavigateToContentInstall += ViewModel_NavigateToContentInstall;
            await ViewModel.Initialize();
        }

        void ViewModel_NavigateToApplicationInstallation(object sender, EventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService.Navigate(new Uri("Views/AppInstallationPage.xaml", UriKind.Relative));
        }

        void ViewModel_NavigateToContentInstall(object sender, EventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService.Navigate(new Uri("Views/ContentInstallationPage.xaml", UriKind.Relative));
        }
    }
}
