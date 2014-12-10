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
    public class ContentInstallationViewModel : BaseViewModel
    {
        
        private ObservableCollection<EventMessageViewModel> _events;
        public ObservableCollection<EventMessageViewModel> Events
        {
            get { return _events; }
            set { SetProperty(ref _events, value); }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
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

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return _cancelCommand; }
            set { SetProperty(ref _cancelCommand, value); }
        }
        
        public ContentInstallationViewModel()
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
            NextCommand = new RelayCommand<object>((e) => { OnNavigateToStart(); });
            CancelCommand = new RelayCommand<object>((e) => { OnCancel(); });

            ContentDeploymentService.Instance.ProgressEvent += Instance_ProgressEvent;
            ContentDeploymentService.Instance.MessagingEvent += Instance_MessagingEvent;
            ContentDeploymentService.Instance.FileUpdateEvent += Instance_FileUpdateEvent;

            string scheme = @"file:///";
            ContentDeploymentService.Instance.DeployContentAsync(new Uri(Path.Combine(scheme, Properties.Settings.Default.ContentFilePath)));
        }

        private void OnCancel()
        {
            if( ContentDeploymentService.Instance.CancelDeployment() )
                OnNavigateToStart();
        }

        public override void Dispose()
        {
            base.Dispose();
            ContentDeploymentService.Instance.ProgressEvent -= Instance_ProgressEvent;
            ContentDeploymentService.Instance.MessagingEvent -= Instance_MessagingEvent;
            ContentDeploymentService.Instance.FileUpdateEvent -= Instance_FileUpdateEvent;
            ContentDeploymentService.Instance.CancelDeployment();
        }

        void Instance_MessagingEvent(object sender, Services.MessageNotificationEventArgs e)
        {
            UpdateUIThreadSafe(() =>
            {
                if (Events.Count() > 10)
                    Events.RemoveAt(Events.Count() - 1);
                Events.Insert(0, new EventMessageViewModel(e.Message, e.TimeStamp));
            });
        }

        void Instance_ProgressEvent(object sender, Services.ProgressUpdateEventArgs e)
        {
            UpdateUIThreadSafe(() =>
            {
                Progress = (int)e.Progress.percentage;
            });
        }

        void Instance_FileUpdateEvent(object sender, Services.FileProgressUpdateEventArgs e)
        {
            string message = string.Empty;
            if (e.Subject == DeploymentSubject.Unpacking)
            {
                message = string.Format("Step 1 of 2: {0} {1} of {2} files.", e.Subject.ToString(), e.CurrentValue, e.TotalValue);
            }
            else if (e.Subject == DeploymentSubject.Installing)
            {
                message = string.Format("Step 2 of 2: {0} {1}% complete.", e.Subject.ToString(), ((double)e.CurrentValue / (double)e.TotalValue) * 100.0);
            }
            else
                throw new ArgumentException(string.Format("{0} was unexpected", e.Subject));

            UpdateUIThreadSafe(() =>
            {
                Message = message;
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
