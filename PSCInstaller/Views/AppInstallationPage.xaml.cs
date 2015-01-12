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
            ViewModel.VisualStateChange -= ViewModel_VisualStateChange;
            ViewModel.NavigateToStart -= ViewModel_NavigateToStart;
            ViewModel.NavigateToContentPackageSelection -= ViewModel_NavigateToContentPackageSelection;
            ViewModel.Dispose();
        }

        async void AppInstallationPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.VisualStateChange += ViewModel_VisualStateChange;
            ViewModel.NavigateToStart += ViewModel_NavigateToStart;
            ViewModel.NavigateToContentPackageSelection += ViewModel_NavigateToContentPackageSelection;

            VisualStateManager.GoToState(mainGrid, AppInstallationViewModel.NormalVisualState, false);

            await ViewModel.Initialize();
        }

        void ViewModel_VisualStateChange(object sender, string visualState)
        {
            VisualStateManager.GoToElementState(mainGrid, visualState, false);
        }

        void ViewModel_NavigateToContentPackageSelection(object sender, EventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService.Navigate(new Uri("Views/ContentPackageSelectionPage.xaml", UriKind.Relative));  
        }

        void ViewModel_NavigateToStart(object sender, EventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService.Navigate(new Uri("Views/StartPage.xaml", UriKind.Relative));
        }
    }
}
