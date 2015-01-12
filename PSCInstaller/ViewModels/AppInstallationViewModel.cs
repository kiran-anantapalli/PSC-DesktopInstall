using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PSCInstaller.Services;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using PSCInstaller.Commands;
namespace PSCInstaller.ViewModels
{
    public class AppInstallationViewModel : BaseViewModel
    {
        public const string NormalVisualState = "Normal";
        public const string InstallAppVisualState = "InstallApp";

        private ObservableCollection<EventMessageViewModel> _events;
        public ObservableCollection<EventMessageViewModel> Events
        {
            get { return _events; }
            set { SetProperty(ref _events, value); }
        }

        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
        }

        private ICommand _nextCommand;
        public ICommand NextCommand
        {
            get { return _nextCommand; }
            set { SetProperty(ref _nextCommand, value); }
        }

        private ICommand _skipApplicationinstallationCommand;
        public ICommand SkipApplicationinstallationCommand
        {
            get { return _skipApplicationinstallationCommand; }
            set { SetProperty(ref _skipApplicationinstallationCommand, value); }
        }

        private ICommand _installApplicationCommand;
        public ICommand InstallApplicationCommand
        {
            get { return _installApplicationCommand; }
            set { SetProperty(ref _installApplicationCommand, value); }
        }   
        
        private ICommand _prevCommand;
        public ICommand PreviousCommand
        {
            get { return _prevCommand; }
            set { SetProperty(ref _prevCommand, value); }
        }

        private bool _isInProgress;
        public bool IsInProgress
        {
            get { return _isInProgress; }
            set { SetProperty(ref _isInProgress, value); }
        }

        public event EventHandler<string> VisualStateChange;
        public void RaiseVisualStateChange(string visualState)
        {
            var handler = VisualStateChange;
            if (handler != null)
                handler(this, visualState);
        }
        public AppInstallationViewModel()
        {
            Events = new ObservableCollection<EventMessageViewModel>();
        }

        //protected override void SetDesignerProperties()
        //{
        //    base.SetDesignerProperties();
        //    InstallationEventMessage = "Installation Failed";
        //    InstallationEventTimeStamp = DateTime.MaxValue;
        //}

        public override async Task Initialize()
        {
            AppRegistrationService.Instance.MessagingEvent += AppInstallManager_OnMessagingEvent;
            AppRegistrationService.Instance.ProgressEvent += AppInstallManager_OnProgressEvent;
            AppRegistrationService.Instance.CompletionEvent += AppInstallManager_CompletionEvent;


            SkipApplicationinstallationCommand = new RelayCommand<object>((e) => { OnNavigateToContentPackageSelection(); });
            InstallApplicationCommand = new RelayCommand<object>((e) => { OnInstallApplication(); });
            NextCommand = new RelayCommand<object>((e) => { OnNavigateToContentPackageSelection(); });
            PreviousCommand = new RelayCommand<object>((e) => { OnNavigateToStart(); });
        }

        public event EventHandler NavigateToStart;
        private void OnNavigateToStart()
        {
            var handler = NavigateToStart;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public event EventHandler NavigateToContentPackageSelection;
        private void OnNavigateToContentPackageSelection()
        {
            var handler = NavigateToContentPackageSelection;
            if (handler != null)
                handler(this, EventArgs.Empty);      
        }

        private async void OnInstallApplication()
        {
            RaiseVisualStateChange(InstallAppVisualState);
            await LoadPackageAsync();
        }

        public override void Dispose()
        {
            AppRegistrationService.Instance.MessagingEvent -= AppInstallManager_OnMessagingEvent;
            AppRegistrationService.Instance.ProgressEvent -= AppInstallManager_OnProgressEvent;
            AppRegistrationService.Instance.CompletionEvent -= AppInstallManager_CompletionEvent;
        }

        private async Task LoadPackageAsync()
        {
            try
            {
                IsInProgress = true;
                string scheme = @"file:///";
                string workingDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
                string packageRelativePath = PSCInstaller.Properties.Settings.Default.PackageRelativePath;
                string appxBundle = PSCInstaller.Properties.Settings.Default.AppBundleName;
                
                var dependencyAppXs = new List<string>();
                foreach (var item in PSCInstaller.Properties.Settings.Default.DependencyPaths)
	            {
		            dependencyAppXs.Add(item);
	            }

                string workingDirectory = Path.Combine(Path.Combine(scheme, workingDirectoryPath), packageRelativePath);
                string appxBundlePath = Path.Combine(workingDirectory, appxBundle);
                var uriAppxBundlePath = new Uri(appxBundlePath, UriKind.RelativeOrAbsolute);

                var uriDependencyPaths = new List<Uri>(){
                    new Uri(Path.Combine(workingDirectory, dependencyAppXs[0]), UriKind.RelativeOrAbsolute),
                    new Uri(Path.Combine(workingDirectory, dependencyAppXs[1]), UriKind.RelativeOrAbsolute),
                    new Uri(Path.Combine(workingDirectory, dependencyAppXs[2]), UriKind.RelativeOrAbsolute),
                };
                await AppRegistrationService.Instance.AddPackageAsync(uriAppxBundlePath.AbsoluteUri, uriDependencyPaths);
            }
            catch (Exception ex)
            {
                AppInstallManager_OnMessagingEvent(this, new MessageNotificationEventArgs(ex.Message));
                IsInProgress = false;
            }
        }

        void AppInstallManager_OnMessagingEvent(object sender, MessageNotificationEventArgs e)
        {
            UpdateUIThreadSafe(() => 
            {
                Events.Add(new EventMessageViewModel(e.Message, e.TimeStamp));
            });
        }

        void AppInstallManager_OnProgressEvent(object sender, ProgressUpdateEventArgs e)
        {
            UpdateUIThreadSafe(() =>
            {
                Progress = (int)e.Progress.percentage;
            });
        }

        void AppInstallManager_CompletionEvent(object sender, bool success)
        {
            UpdateUIThreadSafe(() =>
            {
                IsInProgress = false;
            });
        }
    }

    public class EventMessageViewModel : BaseViewModel
    {
        private DateTime _installationEventTimeStamp;
        public DateTime InstallationEventTimeStamp
        {
            get { return _installationEventTimeStamp; }
            set { SetProperty(ref _installationEventTimeStamp, value); }
        }

        private string _installationEventMessage;
        public string InstallationEventMessage
        {
            get { return _installationEventMessage; }
            set { SetProperty(ref _installationEventMessage, value); }
        }

        public EventMessageViewModel() { }
        public EventMessageViewModel(string message)
            : this(message, DateTime.Now)
        {
        }
        public EventMessageViewModel(string message, DateTime timestamp)
        {
            InstallationEventMessage = message;
            InstallationEventTimeStamp = timestamp;
        }
    }
}
