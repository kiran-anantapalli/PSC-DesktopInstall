using PSCInstaller.Commands;
using PSCInstaller.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PSCInstaller.ViewModels
{
    public class UnInstallViewModel : BaseViewModel
    {

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

        public UnInstallViewModel()
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
            AppRegistrationService.Instance.MessagingEvent += AppInstallManager_OnMessageNotified;
            AppRegistrationService.Instance.ProgressEvent += AppInstallManager_OnProgressEvent;
            NextCommand = new RelayCommand<object>((e) => { OnNavigateToStart(); });
            await LoadPackage();
        }

        private async Task LoadPackage()
        {
            await Task.Yield();

            try
            {
                var packageDatamodel = AppRegistrationService.Instance.FindPackage(  PSCInstaller.Properties.Settings.Default.PackageName);

                await AppRegistrationService.Instance.RemovePackageAsync(packageDatamodel.AppPackageFullName);
            }
            catch (Exception ex)
            {
                AppInstallManager_OnMessageNotified(this, new MessageNotificationEventArgs(ex.Message));
            }
        }


        public override void Dispose()
        {
            AppRegistrationService.Instance.MessagingEvent -= AppInstallManager_OnMessageNotified;
            AppRegistrationService.Instance.ProgressEvent -= AppInstallManager_OnProgressEvent;
        }

        void AppInstallManager_OnMessageNotified(object sender, MessageNotificationEventArgs e)
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

        public event EventHandler NavigateToStart;
        private void OnNavigateToStart()
        {
            var handler = NavigateToStart;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }

}
