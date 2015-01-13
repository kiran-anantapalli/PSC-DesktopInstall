using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using PSCInstaller.Commands;
using PSCInstaller.Services;
namespace PSCInstaller.ViewModels
{
    public class StartViewModel : BaseViewModel
    {
        private ICommand _navigateToInstallationCommand;
        public ICommand NavigateToInstallationCommand
        {
            get { return _navigateToInstallationCommand; }
            set { SetProperty(ref _navigateToInstallationCommand, value); }
        }

        private ICommand _navigateToUnInstallCommand;
        public ICommand NavigateToUnInstallCommand
        {
            get { return _navigateToUnInstallCommand; }
            set { SetProperty(ref _navigateToUnInstallCommand, value); }
        }

        private bool _isPackageInstalled;
        public bool IsPackageInstalled
        {
            get { return _isPackageInstalled; }
            set { SetProperty(ref _isPackageInstalled, value); }
        }

        public StartViewModel()
            : base()
        {
            NavigateToInstallationCommand = new RelayCommand<object>((e) => { OnNavigateToInstall(); });

            NavigateToUnInstallCommand = new RelayCommand<object>(  (e) => { OnNavigateToUnInstall(); },
                                                                    (e) => { return IsPackageInstalled; });

        }

        public override async System.Threading.Tasks.Task Initialize()
        {
            await Task.Yield();
            IsPackageInstalled = true;// null != AppRegistrationService.Instance.FindPackage(Properties.Settings.Default.PackageName);
        }

        public event EventHandler NavigateToUnInstall;
        private void OnNavigateToUnInstall()
        {
            var handler = NavigateToUnInstall;
            if (handler != null)
                handler(this, EventArgs.Empty);  
        }

        public event EventHandler NavigateToInstall;
        private void OnNavigateToInstall()
        {
            var handler = NavigateToInstall;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

    }
}
