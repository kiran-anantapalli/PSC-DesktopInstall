using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Management.Deployment;

namespace PSCInstaller.Services
{
    public class MessageNotificationEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public MessageNotificationEventArgs(string eventMessage, DateTime timestamp)
        {
            Message = eventMessage;
            TimeStamp = timestamp;
        }

        public MessageNotificationEventArgs(string eventMessage)
            : this(eventMessage, DateTime.Now)
        {
        }
    }


    public class ProgressUpdateEventArgs : EventArgs
    {
        public DeploymentProgress Progress { get; private set; }
        public ProgressUpdateEventArgs( DeploymentProgress progress)
        {
            Progress = progress;
        }
    }

    //public delegate void DeploymentProgressEventHandler(object sender, ProgressUpdateEventArgs e);
    //public delegate void MessageNotificationEventHandler(object sender, MessageNotificationEventArgs e);

}
