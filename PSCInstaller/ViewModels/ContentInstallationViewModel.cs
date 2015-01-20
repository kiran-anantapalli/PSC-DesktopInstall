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

        private string _estimatedTimeRemaining;
        public string EstimatedTimeRemaining
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
        private void OnNext()
        {
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

                // Only update every 10seconds
                if (!_lastProgressUpdate.HasValue || now.Subtract(_lastProgressUpdate.Value).TotalSeconds >= 10)
                {
                    _lastProgressUpdate = now;

                    var elapsedTime = now.Subtract(_startOfInstallation);
                    var remainingPercentage = e.Progress.percentage == 0 ? 100 : ((double)100 / (double)e.Progress.percentage) - 1.0;
                    var estimatedTime = TimeSpan.FromSeconds(elapsedTime.TotalSeconds * remainingPercentage);
                    EstimatedTimeRemaining = "Est. Time of Completion: " + now.Add(estimatedTime).ToShortTimeString();
                }
            });
        }

        void Instance_FileUpdateEvent(object sender, Services.FileProgressUpdateEventArgs e)
        {
            string message = string.Empty;
            int stepNumber = 1;
            if (e.Subject == DeploymentSubject.Installing)
                stepNumber = 2;
            message = string.Format("Step {0} of 2: {1} ({2:f0}%).",stepNumber, e.Subject.ToString(), ((double)e.CurrentValue / (double)e.TotalValue) * 100.0);

            UpdateUIThreadSafe(() =>
            {
                Message = message;
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
