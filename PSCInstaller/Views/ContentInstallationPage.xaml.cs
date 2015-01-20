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
    public partial class ContentInstallationPage : Page
    {
        public ContentInstallationViewModel ViewModel
        {
            get { return DataContext as ContentInstallationViewModel; }
        }

        public ContentInstallationPage()
        {
            DataContext = new ContentInstallationViewModel();
            InitializeComponent();
            this.Loaded += ContentInstallationPage_Loaded;
            this.Unloaded += ContentInstallationPage_Unloaded;
        }

        void ContentInstallationPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NavigateToContentPackageSelection -= ViewModel_NavigateToContentSelection;
            ViewModel.NavigateToStart -= ViewModel_NavigateToStart;
            ViewModel.Dispose();
        }

        async void ContentInstallationPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NavigateToContentPackageSelection += ViewModel_NavigateToContentSelection;
            ViewModel.NavigateToStart += ViewModel_NavigateToStart;
            await ViewModel.Initialize();
        }

        void ViewModel_NavigateToStart(object sender, EventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService.Navigate(new Uri("Views/StartPage.xaml", UriKind.Relative));
        }

        void ViewModel_NavigateToContentSelection(object sender, EventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService.Navigate(new Uri("Views/ContentPackageSelectionPage.xaml", UriKind.Relative));
        }
    }
}
