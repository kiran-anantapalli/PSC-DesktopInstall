using System.Diagnostics;
using System.Windows;
using PSCInstaller.Commands;
using PSCInstaller.Sevices;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SevenZip;

namespace PSCInstaller.ViewModels
{


    public class DeployContentViewModel : BaseViewModel
    {

        #region properties

        private string _errorText;
        public string ErrorText
        {
            get { return _errorText; }
            set { SetProperty(ref _errorText, value); }
        }

        private string _deploymentFinishedText;
        public string DeploymentFinishedText
        {
            get { return _deploymentFinishedText; }
            set { SetProperty(ref _deploymentFinishedText, value); }
        }

        private bool _isDeployRunning;
        public bool IsDeployRunning
        {
            get { return _isDeployRunning; }
            set { SetProperty(ref _isDeployRunning, value); }
        }

        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
        }

        private ICommand _deployCommand;
        public ICommand DeployCommand
        {
            get { return _deployCommand; }
            set { SetProperty(ref _deployCommand, value); }
        }

        private ICommand _closeAppCommand;
        public ICommand CloseAppCommand
        {
            get { return _closeAppCommand; }
            set { SetProperty(ref _closeAppCommand, value); }
        }

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

        #endregion

        public override async Task Initialize()
        {
            DeployCommand = new RelayCommand<object>((e) => OnDeploy());
            CloseAppCommand = new RelayCommand<object>((e) => OnCloseApp());

            ContentDeploymentService.Instance.ProgressEvent += Instance_ProgressEvent;

            this.IsDeployRunning = false;
            this.ErrorText = string.Empty;
        }

        private async void OnDeploy()
        {
            this.ErrorText = string.Empty;

            var success = CheckIfContentIsAlreadyInstalled();
            if (!success)
            {
                return;
            }
            success = KillRunningProcess();
            if (!success)
            {
                return;
            }
            success = SetContentPackageFilePath();
            if (!success)
            {
                return;
            }
            success = CheckFreeSpace();
            if (!success)
            {
                return;
            }

            this.IsDeployRunning = true;
            success = await ExtractContent();
            if (!success)
            {
                return;
            }

            success = CopyOverrideFlag();
            if (!success)
            {
                ErrorText = "An error while marking the content as installed";
            }
        }

        private bool CopyOverrideFlag()
        {
            var path = Path.Combine("PSC", Properties.Settings.Default.OverwriteFlagFileName);

            ContentDeploymentService.Instance.CopyOverrideFlag(path);

            return true;
        }

        private async Task<bool> ExtractContent()
        {
            await Task.Delay(500);
            int exitCode = await ContentDeploymentService.Instance.DeployContentAsync();
            await Task.Delay(500);
            switch (exitCode)
            {
                case 255:
                    this.DeploymentFinishedText = "User cancelled the process";
                    return false;
                case 8:
                    this.DeploymentFinishedText = "Not enough memory to complete the process";
                    return false;
                case 7:
                    this.DeploymentFinishedText = "Command line error";
                    return false;
                case 2:
                    this.DeploymentFinishedText = "Fatal error";
                    return false;
                case 1:
                    this.DeploymentFinishedText = "Content successfully deployed, but with warnings";
                    break;
                default:
                    this.DeploymentFinishedText = "Content successfully deployed";
                    break;
            }

            return true;
        }

        private bool CheckFreeSpace()
        {
            long freeSpace = 0L; 
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name.Contains("C:\\"))
                {
                    freeSpace = drive.TotalFreeSpace;
                }
            }

            long zipSpace = GetZipFileSize();
            if (freeSpace != 0L && (zipSpace >= freeSpace))
            {
                MessageBox.Show(String.Format("You need {0} bytes fee in order to install the application\nYou currently have {1} bytes free", zipSpace, freeSpace), "Confirmation", MessageBoxButton.OK, MessageBoxImage.Hand);
                return false;
            }
            return true;
        }


        private long GetZipFileSize()
        {
            SevenZipBase.SetLibraryPath("7z.dll");
            
            var szip = new SevenZipExtractor(ContentDeploymentService.Instance.ContentPackageFilePath);

            return szip.PackedSize;
        }

        private bool KillRunningProcess()
        {
            try
            {
                var processes = Process.GetProcessesByName(Properties.Settings.Default.ProcessName);
                foreach (var process in processes)
                {
                    process.Kill();
                }
            }
            catch
            {
                ErrorText = "An error occured when trying to kill app";
                return false;
            }
            return true;
        }

        private bool CheckIfContentIsAlreadyInstalled()
        {
            string localStateFolder = ContentDeploymentService.Instance.GetLocalStateFolder();
            string targetDirectoryPath = Path.Combine(localStateFolder, Properties.Settings.Default.OverwriteFlagFileName);
            
            if (File.Exists(targetDirectoryPath))
            {
                var result = MessageBox.Show("Would you like to overwrite existing content?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    return true;
                }
                return false;
            }
            return true;
        }

        private bool SetContentPackageFilePath()
        {
            var pscFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PSC");
            if (!Directory.Exists(pscFolder))
            {
                this.ErrorText = String.Format("No PSC folder found");
                return false;
            }
            var filesInFolder = Directory.GetFiles(pscFolder).Where(x => x.EndsWith(".7z")).ToList();

            if (filesInFolder.Count() > 1)
            {
                this.ErrorText = String.Format("More than one .7z file found in {0}", pscFolder);
                return false;
            }
            if (!filesInFolder.Any())
            {
                this.ErrorText = String.Format("No .7z file found in {0}", pscFolder);
                return false;
            }

            ContentDeploymentService.Instance.ContentPackageFilePath = filesInFolder[0];
            return true;
        }

        private void OnCloseApp()
        {
            Application.Current.Shutdown();
        }

        

        public override void Dispose()
        {
            base.Dispose();
            ContentDeploymentService.Instance.ProgressEvent -= Instance_ProgressEvent;
            ContentDeploymentService.Instance.CancelDeployment();
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
                }
            });
        }


    }
}
