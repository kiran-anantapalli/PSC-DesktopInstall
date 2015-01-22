using PSCInstaller.Commands;
using PSCInstaller.Sevices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using Windows.Storage;
using Windows.Foundation;
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

        private string _messagePrefix;
        public string MessagePrefix
        {
            get { return _messagePrefix; }
            set { SetProperty(ref _messagePrefix, value); }
        }

        private string _estimatedTimeRemaining;
        public string EstimatedTimeRemainingMessage
        {
            get { return _estimatedTimeRemaining; }
            set { SetProperty(ref _estimatedTimeRemaining, value); }
        }

        private string _contentFileName;
        public string ContentFileName
        {
            get { return _contentFileName; }
            set { SetProperty(ref _contentFileName, value); }
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


        private DateTime _startOfInstallation;

        private object lastProgresUpdateLock = new object();
        private DateTime? _lastProgressUpdate;
        public DateTime? LastProgressUpdate
        {
            get
            {
                lock (lastProgresUpdateLock)
                {
                    return _lastProgressUpdate;
                }
            }
            private set 
            {
                lock (lastProgresUpdateLock)
                {
                    _lastProgressUpdate = value;
                }
            }
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
            NextCommand = new RelayCommand<object>((e) => { OnNext(); });
            CancelCommand = new RelayCommand<object>((e) => { OnCancel(); });

            ContentDeploymentService.Instance.ProgressEvent += Instance_ProgressEvent;
            ContentDeploymentService.Instance.MessagingEvent += Instance_MessagingEvent;
            ContentDeploymentService.Instance.FileUpdateEvent += Instance_FileUpdateEvent;

            _startOfInstallation = DateTime.Now;
            ContentDeploymentService.Instance.DeployContentAsync();

            ContentFileName = Path.GetFileName(ContentDeploymentService.Instance.ContentPackageFilePath);
        }

        public event EventHandler NavigateToStart;
        private async void OnNext()
        {
            var fullFilePath = System.IO.Path.GetFullPath("launcher.ccsoc");
            System.Diagnostics.Process.Start(fullFilePath);

            var handler = NavigateToStart;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void OnCancel()
        {
            if( ContentDeploymentService.Instance.CancelDeployment() )
                OnNavigateToContentPackageSelection();
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
            var now = DateTime.Now;
            UpdateUIThreadSafe(() =>
            {
                Progress = (int)e.Progress.percentage;

                if (!_lastProgressUpdate.HasValue)
                {
                    _lastProgressUpdate = now;
                    return;
                }

                // Only update every 10seconds
                if (now.Subtract(_lastProgressUpdate.Value).TotalSeconds >= 10)
                {
                    _lastProgressUpdate = now;
                    var elapsedTime = now.Subtract(_startOfInstallation);
                    var remainingPercentage = e.Progress.percentage == 0 ? 100 : ((double)100 / (double)e.Progress.percentage) - 1.0;
                    var estimatedTime = TimeSpan.FromSeconds(elapsedTime.TotalSeconds * remainingPercentage);
                    //EstimatedTimeRemainingMessage = "Complete by: " + now.Add(estimatedTime).ToShortTimeString();
                    EstimatedTimeRemainingMessage = ((int)estimatedTime.TotalMinutes) + " minutes remaining";
                }
            });
        }

        void Instance_FileUpdateEvent(object sender, Services.FileProgressUpdateEventArgs e)
        {
            string message = string.Empty;
            string messagePrefix = string.Empty;
            int stepNumber = 1;
            if (e.Subject == DeploymentSubject.Installing)
                stepNumber = 2;
            messagePrefix = string.Format("Step {0} of 2:", stepNumber);
            message = string.Format("{0} ({1:f0}%)",e.Subject.ToString(), ((double)e.CurrentValue / (double)e.TotalValue) * 100.0);

            UpdateUIThreadSafe(() =>
            {
                Message = message;
                MessagePrefix = messagePrefix;
            });
        }
        
        public event EventHandler NavigateToContentPackageSelection;
        private void OnNavigateToContentPackageSelection()
        {
            var handler = NavigateToContentPackageSelection;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
