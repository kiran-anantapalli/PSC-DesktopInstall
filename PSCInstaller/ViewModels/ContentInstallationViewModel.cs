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

        public ContentInstallationViewModel()
        {
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

            ContentDeploymentService.Instance.ProgressEvent += Instance_ProgressEvent;
            ContentDeploymentService.Instance.MessagingEvent += Instance_MessagingEvent;

            string scheme = @"file:///";
            await ContentDeploymentService.Instance.DeployContent(new Uri(Path.Combine(scheme, Properties.Settings.Default.ContentFilePath)));
        }

        public override void Dispose()
        {
            base.Dispose();
            ContentDeploymentService.Instance.ProgressEvent -= Instance_ProgressEvent;
            ContentDeploymentService.Instance.MessagingEvent -= Instance_MessagingEvent;
        }

        void Instance_MessagingEvent(object sender, Services.MessageNotificationEventArgs e)
        {
            UpdateUIThreadSafe(() =>
            {
                Message = e.Message;
            });
        }

        void Instance_ProgressEvent(object sender, Services.ProgressUpdateEventArgs e)
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
