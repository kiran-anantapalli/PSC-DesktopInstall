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
    /// Interaction logic for Page3.xaml
    /// </summary>
    public partial class UnInstallPage : Page
    {
        public UnInstallViewModel ViewModel
        {
            get { return DataContext as UnInstallViewModel; }
        }

        public UnInstallPage()
        {
            DataContext = new UnInstallViewModel();
            InitializeComponent();
            this.Loaded += UnInstallPage_Loaded;
            this.Unloaded += UnInstallPage_Unloaded;
        }

        void UnInstallPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NavigateToStart -= ViewModel_NavigateToStart;
            ViewModel.Dispose();
        }

        async void UnInstallPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NavigateToStart += ViewModel_NavigateToStart;
            await ViewModel.Initialize();
        }

        void ViewModel_NavigateToStart(object sender, EventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService.Navigate(new Uri("Views/StartPage.xaml", UriKind.Relative));
        }
    }
}
