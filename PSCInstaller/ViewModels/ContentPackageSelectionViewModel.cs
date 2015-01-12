using PSCInstaller.Commands;
using PSCInstaller.Sevices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PSCInstaller.ViewModels
{
    public class ContentPackageSelectionViewModel : BaseViewModel
    {
        private ObservableCollection<string> _packages;
        public ObservableCollection<string> Packages
        {
            get { return _packages; }
            set { SetProperty(ref _packages, value); }
        }

        private ICommand _nextCommand;
        public ICommand NextCommand
        {
            get { return _nextCommand; }
            set { SetProperty(ref _nextCommand, value); }
        }

        private ICommand _previousCommand;
        public ICommand PreviousCommand
        {
            get { return _previousCommand; }
            set { SetProperty(ref _previousCommand, value); }
        }
        
        private int _currentPackageIndex;
        public int CurrentPackageIndex
        {
            get { return _currentPackageIndex; }
            set { SetProperty(ref _currentPackageIndex, value); }
        }

        public ContentPackageSelectionViewModel()
        {
            Packages = new ObservableCollection<string>();
        }

        public override async Task Initialize()
        {

            PreviousCommand = new RelayCommand<object>((e) => { OnNavigateToApplicationInstallation(); });

            NextCommand = new RelayCommand<object>((e) => { OnNavigateToContentInstall(); },
                                                   (e) => { return CurrentPackageIndex >= 0; });

            for (int i = 0; i < Properties.Settings.Default.ContentFilePaths.Count; i++)
            {
                if (File.Exists(Properties.Settings.Default.ContentFilePaths[i]))
                    Packages.Add(Properties.Settings.Default.ContentFilePaths[i]);
            }
        }

        public event EventHandler NavigateToApplicationInstallation;
        private void OnNavigateToApplicationInstallation()
        {
            var handler = NavigateToApplicationInstallation;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public event EventHandler NavigateToContentInstall;
        private void OnNavigateToContentInstall()
        {
            var handler = NavigateToContentInstall;
            if (handler != null)
                handler(this, EventArgs.Empty);

            ContentDeploymentService.Instance.ContentPackageFilePath = Packages[CurrentPackageIndex];
        }
    }
}
