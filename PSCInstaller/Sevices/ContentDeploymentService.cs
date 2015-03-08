using System;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using PSCInstaller.Services;
using System.IO;
using Windows.Management.Deployment;
using System.Threading;

namespace PSCInstaller.Sevices
{
    public enum DeploymentSubject
    {
        Unpacking,
        Installing
    }

    public class ContentDeploymentService : IProgress<ProgressUpdateEventArgs>
    {
        public const int ProgressReportCycleThreshHold = 3;
        private long _temporaryDirectorySize;
        private long _transferedDirectorySize;
        private CancellationTokenSource _cancelSource;

        public string ContentPackageFilePath { get; set; }

        #region Singleton

        public static readonly ContentDeploymentService Instance = new ContentDeploymentService();

        private ContentDeploymentService()
        {
        }

        #endregion

        public string GetLocalStateFolder()
        {
            string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string appDataLocalStateDirectory = @"AppData\Local\Packages\" + Properties.Settings.Default.PackageName + "_" + Properties.Settings.Default.PublisherId + @"\LocalState";
            return Path.Combine(userDirectory, appDataLocalStateDirectory);
        }

        public async Task CopyOverrideFlag(string flagPath)
        {
            string targetDirectoryPath = Path.Combine(GetLocalStateFolder(), Properties.Settings.Default.OverwriteFlagFileName);
            
            File.Copy(flagPath, targetDirectoryPath, true);
            RaiseMessage("Installation Complete");
        }

        public async Task<int> DeployContentAsync()
        {
            Uri packageUri = null;
            string scheme = @"file:///";
            string workingDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
            if (!Path.IsPathRooted(Instance.ContentPackageFilePath))
            {
                var absolutePath = Path.Combine(Path.Combine(scheme, workingDirectoryPath), Instance.ContentPackageFilePath);
                packageUri = new Uri(absolutePath);
            }
            else
            {
                var absolutePath = Path.Combine(scheme, Instance.ContentPackageFilePath);
                packageUri = new Uri(absolutePath);
            }

            _cancelSource = new CancellationTokenSource();
            //var extractDirectory = await UnzipToTemporaryLocationAsync(packageUri);
            return await SevenZipToContentLocationAsync(packageUri);
            //await CopyToUserPackageInstallationDirectoryAsync(extractDirectory);
            //await CleanupDirectoryAsync(extractDirectory);
            //RaiseMessage("Installation Complete");
        }

        private async Task CleanupDirectoryAsync(string directory)
        {
            await Task.Run(() =>
            {
                RaiseMessage("Cleaning up...");
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            });
        }

        private async Task<int> SevenZipToContentLocationAsync(Uri zipFileUri)
        {
            double percentComplete = 0;
            RaiseProgress(percentComplete);
            string extractDirectory = string.Empty;
            int exitCode = 0;

            try
            {
                string targetDirectoryPath = GetLocalStateFolder();

                string archiveName = zipFileUri.Segments.Last();
                if (Path.HasExtension(archiveName))
                {
                    archiveName = archiveName.Remove(archiveName.IndexOf(Path.GetExtension(archiveName)));
                }
                extractDirectory = targetDirectoryPath;
                //await CleanupDirectoryAsync(extractDirectory);
                RaiseMessage("Unpacking Content");

                var executable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7zg.exe");

                Directory.CreateDirectory(extractDirectory);

                var arguments = " x -y -o\"" + extractDirectory + "\" \"" + zipFileUri.LocalPath + "\"";

                // Extract files
                var startInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    Arguments = arguments
                };

                var process = Process.Start(startInfo);
                process.WaitForExit();

                exitCode = process.ExitCode;              

                RaiseProgress(100);
            }
            catch (Exception ex)
            {
                RaiseMessage(string.Format("Error Unpacking: {0}", ex.Message));
            }

            RaiseMessage("Unpacking Complete");
            return exitCode;
        }
        

        //private async Task<string> UnzipToTemporaryLocationAsync(Uri zipFileUri)
        //{
        //    double perfectComplete = 0;
        //    RaiseProgress(perfectComplete);
        //    string extractDirectory = string.Empty;
        //    try
        //    {
        //        string tempPath = System.IO.Path.GetTempPath();
        //        string archiveName = zipFileUri.Segments.Last();
        //        if( Path.HasExtension(archiveName) )
        //        {
        //            archiveName = archiveName.Remove(archiveName.IndexOf(Path.GetExtension(archiveName)));
        //        }

        //        extractDirectory = Path.Combine(tempPath, "_" + archiveName);
        //        await CleanupDirectoryAsync(extractDirectory);

        //        RaiseMessage("Unpacking Content");
        //        var di = Directory.CreateDirectory(extractDirectory);
        //        using (var fs = new FileStream(zipFileUri.AbsolutePath, FileMode.Open))
        //        {
        //            using (ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Read) )
        //            {
        //                double unitsComplete = 0;
        //                double totalUnits = arch.Entries.Count();
        //                foreach (var item in arch.Entries)
        //                {
        //                    if( _cancelSource.IsCancellationRequested)
        //                    {
        //                        RaiseMessage("Process has been canceled");
        //                        break;
        //                    }

        //                    RaiseMessage(string.Format("Unpacking: {0}", item.Name));
        //                    RaiseFileUpdate((long)unitsComplete, (long)totalUnits, DeploymentSubject.Unpacking);
        //                    if (item.Length == 0)
        //                    {
        //                        var rootPathRemoved = item.FullName.Substring(item.FullName.IndexOf("/") + 1);
        //                        string directoryPath = Path.Combine(di.FullName, rootPathRemoved);
        //                        string filepath = Path.Combine(di.FullName, directoryPath);
        //                        Directory.CreateDirectory(directoryPath);
        //                    }
        //                    else
        //                    {
        //                        byte[] buffer = new byte[item.Length];
        //                        using (var entryStream = item.Open())
        //                        {
        //                            int bytesRead = 0;
        //                            do
        //                            {
        //                                bytesRead += await entryStream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);
        //                            } while (bytesRead < buffer.Length);
        //                        }

        //                        var rootPathRemoved = item.FullName.Substring(item.FullName.IndexOf("/") + 1);
        //                        string filepath = Path.Combine(di.FullName, rootPathRemoved);
        //                        using (var writeStream = File.OpenWrite(filepath))
        //                        {
        //                            await writeStream.WriteAsync(buffer, 0, buffer.Count());
        //                        }
        //                    }

        //                    if( ++unitsComplete % ProgressReportCycleThreshHold == 0 )
        //                    {
        //                        perfectComplete = ((unitsComplete / totalUnits) * 100.0) * 0.5;
        //                        RaiseProgress(perfectComplete);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        RaiseMessage(string.Format("Error Unpacking: {0}", ex.Message));
        //    }

        //    RaiseMessage("Unpacking Complete");
        //    return extractDirectory;
        //}

        private async Task CopyToUserPackageInstallationDirectoryAsync(string contentDirectory)
        {
            if (!Directory.Exists(contentDirectory))
                throw new InvalidOperationException(string.Format("directory {0} does not exist", contentDirectory));

            if (_cancelSource == null || _cancelSource.IsCancellationRequested)
                return;

            RaiseProgress(0);
            RaiseMessage("Installing Content");

            string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string appDataLocalStateDirectory = @"AppData\Local\Packages\" + Properties.Settings.Default.PackageName + "_" + Properties.Settings.Default.PublisherId + @"\LocalState";
            string targetDirectoryPath = Path.Combine(userDirectory, appDataLocalStateDirectory);

            _transferedDirectorySize = 0;
            _temporaryDirectorySize = GetDirectorySize(contentDirectory);

            RaiseFileUpdate(_transferedDirectorySize, _temporaryDirectorySize, DeploymentSubject.Installing);
            await Task.Run(async () =>
            {
                await CopyDirectory(contentDirectory, targetDirectoryPath);
            }, _cancelSource.Token);
        }

        private long GetDirectorySize(string rootDirectory)
        {
            long size = 0;
            var v = new DirectoryInfo(rootDirectory);
            foreach (var directory in v.EnumerateDirectories())
                size += GetDirectorySize(directory.FullName);

            size += v.EnumerateFiles().Sum(f => f.Length);
            return size;
        }

        private async Task CopyDirectory(string sourceDirectoryPath, string targetDirectoryPath)
        {
            //copy /Y /V "%scriptpath%PSC\_overwrite.flg" "%UserProfile%\AppData\Local\Packages\Pearson.PSC_2dnf9zyx96z42\LocalState"
            try
            {
                // copy files first
                var fileNames = Directory.GetFiles(sourceDirectoryPath);
                foreach (var filePath in fileNames)
                {
                    if (_cancelSource.IsCancellationRequested)
                        return;

                    var fileName = Path.GetFileName(filePath);
                    var destFilePath = Path.Combine(targetDirectoryPath, fileName);
                    File.Copy(filePath, destFilePath, true);

                    var fi = new FileInfo(filePath);
                    _transferedDirectorySize += fi.Length;

                    double perfectComplete = ((((double)_transferedDirectorySize / (double)_temporaryDirectorySize) * 100.0) * 0.5) + 50.0;
                    RaiseProgress(perfectComplete);
                    RaiseMessage(string.Format("Installing: {0}", fileName));
                    RaiseFileUpdate(_transferedDirectorySize, _temporaryDirectorySize, DeploymentSubject.Installing);
                }

                // create sub directories
                var directories = Directory.GetDirectories(sourceDirectoryPath, "*", SearchOption.TopDirectoryOnly);
                foreach (var directory in directories)
                {
                    if (_cancelSource.IsCancellationRequested)
                        return;

                    var directoryName = Path.GetFileName(directory);
                    var destDirectoryPath = System.IO.Path.Combine(targetDirectoryPath, directoryName);

                    if (!Directory.Exists(destDirectoryPath))
                        Directory.CreateDirectory(destDirectoryPath);

                    var targetSubDirectoryPath = Path.Combine(targetDirectoryPath, directoryName);
                    await CopyDirectory(directory, targetSubDirectoryPath);
                }
            }
            catch (Exception ex)
            {
                RaiseMessage(string.Format("Error: {0}", ex.Message));
            }
        }

        public bool CancelDeployment()
        {
            if (_cancelSource == null)
                return true;

            _cancelSource.Cancel();
            var result = _cancelSource.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));
            if (!result)
                RaiseMessage("Deployment cancellation timed out");
            _cancelSource.Dispose();
            _cancelSource = null;
            return result;
        }

        public event EventHandler<ProgressUpdateEventArgs> ProgressEvent;
        public void Report(ProgressUpdateEventArgs e)
        {
            var handler = ProgressEvent;
            if (handler != null)
                handler(this, e);
        }
        public void RaiseProgress(double progress)
        {
            Report(new ProgressUpdateEventArgs(new DeploymentProgress() { percentage = (uint)progress, state = DeploymentProgressState.Processing }));
        }

        public event EventHandler<MessageNotificationEventArgs> MessagingEvent;
        public void RaiseMessage(string message)
        {
            var handler = MessagingEvent;
            if (handler != null)
                handler(this, new MessageNotificationEventArgs(message));
        }

        public event EventHandler<FileProgressUpdateEventArgs> FileUpdateEvent;
        public void RaiseFileUpdate(long fileNo, long totalFiles, DeploymentSubject subject)
        {
            var handler = FileUpdateEvent;
            if (handler != null)
                handler(this, new FileProgressUpdateEventArgs(fileNo, totalFiles, subject));
        }
    }
}
