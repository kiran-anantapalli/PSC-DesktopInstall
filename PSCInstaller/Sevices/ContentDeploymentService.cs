using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PSCInstaller.Services;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using Windows.Management.Deployment;
namespace PSCInstaller.Sevices
{
    public class ContentDeploymentService 
        : IProgress<ProgressUpdateEventArgs>
    {
        public const int ProgressReportCycleThreshHold = 3;

        private long _temporaryDirectorySize;
        private long _transferedDirectorySize;

        #region Singleton

        public static readonly ContentDeploymentService Instance = new ContentDeploymentService();

        private ContentDeploymentService()
        {
        }

        #endregion

        public async Task DeployContent(Uri contentPackageUri)
        {
            var extractDirectory = await UnzipToTemporaryLocation(contentPackageUri);
            
            await CopyToUserPackageInstallationDirectory(extractDirectory);

            RaiseMessage("Cleaning up...");
            if (Directory.Exists(extractDirectory))
                Directory.Delete(extractDirectory, true);
            
            RaiseMessage("Installation Complete");
        }

        private async Task<string> UnzipToTemporaryLocation(Uri zipFileUri)
        {
            double perfectComplete = 0;
            RaiseProgress(perfectComplete);
            RaiseMessage("Unpacking Content");
            string extractDirectory = string.Empty;
            try
            {
                string tempPath = System.IO.Path.GetTempPath();
                string archiveName = zipFileUri.Segments.Last();
                if( Path.HasExtension(archiveName) )
                {
                    archiveName = archiveName.Remove(archiveName.IndexOf(Path.GetExtension(archiveName)));
                }

                extractDirectory = Path.Combine(tempPath, "_" + archiveName);
                if (Directory.Exists(extractDirectory))
                    Directory.Delete(extractDirectory, true);
                var di = Directory.CreateDirectory(extractDirectory);

                using (var fs = new FileStream(zipFileUri.AbsolutePath, FileMode.Open))
                {
                    using (ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Read) )
                    {
                        double unitsComplete = 0;
                        double totalUnits = arch.Entries.Count();
                        foreach (var item in arch.Entries)
                        {
                            RaiseMessage(string.Format("Unpacking: {0}", item.Name));
                            if (item.Length == 0)
                            {
                                var rootPathRemoved = item.FullName.Substring(item.FullName.IndexOf("/") + 1);
                                string directoryPath = Path.Combine(di.FullName, rootPathRemoved);
                                string filepath = Path.Combine(di.FullName, directoryPath);
                                Directory.CreateDirectory(directoryPath);
                            }
                            else
                            {
                                byte[] buffer = new byte[item.Length];
                                using (var entryStream = item.Open())
                                {
                                    int bytesRead = 0;
                                    do
                                    {
                                        bytesRead += await entryStream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);
                                    } while (bytesRead < buffer.Length);
                                }

                                var rootPathRemoved = item.FullName.Substring(item.FullName.IndexOf("/") + 1);
                                string filepath = Path.Combine(di.FullName, rootPathRemoved);
                                using (var writeStream = File.OpenWrite(filepath))
                                {
                                    await writeStream.WriteAsync(buffer, 0, buffer.Count());
                                }
                            }

                            if( ++unitsComplete % ProgressReportCycleThreshHold == 0 )
                            {
                                perfectComplete = ((unitsComplete / totalUnits) * 100.0) * 0.5;
                                RaiseProgress(perfectComplete);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseMessage(string.Format("Error Unpacking: {0}", ex.Message));
            }

            RaiseMessage("Unpacking Complete");
            return extractDirectory;
        }

        private async Task CopyToUserPackageInstallationDirectory(string contentDirectory)
        {
            if (!Directory.Exists(contentDirectory))
                throw new InvalidOperationException(string.Format("directory {0} does not exist", contentDirectory));

            RaiseProgress(0);
            RaiseMessage("Installing Content");

            string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string appDataLocalStateDirectory = @"AppData\Local\Packages\" + Properties.Settings.Default.PackageName + "_" + Properties.Settings.Default.PublisherId + @"\LocalState";
            string targetDirectoryPath = Path.Combine(userDirectory, appDataLocalStateDirectory);

            _transferedDirectorySize = 0;
            _temporaryDirectorySize = GetDirectorySize(contentDirectory);

            await Task.Run(() =>
            {
                CopyDirectory(contentDirectory, targetDirectoryPath);
            });
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

        private void CopyDirectory(string sourceDirectoryPath, string targetDirectoryPath)
        {
            //copy /Y /V "%scriptpath%PSC\_overwrite.flg" "%UserProfile%\AppData\Local\Packages\Pearson.PSC_2dnf9zyx96z42\LocalState"
            try
            {
                // copy files first
                var fileNames = Directory.GetFiles(sourceDirectoryPath);
                foreach (var filePath in fileNames)
                {
                    var fileName = Path.GetFileName(filePath);
                    var destFilePath = System.IO.Path.Combine(targetDirectoryPath, fileName);
                    File.Copy(filePath, destFilePath, true);

                    var fi = new FileInfo(filePath);
                    _transferedDirectorySize += fi.Length;

                    double perfectComplete = ((((double)_transferedDirectorySize / (double)_temporaryDirectorySize) * 100.0) * 0.5) + 50.0;
                    RaiseProgress(perfectComplete);
                    RaiseMessage(string.Format("Installing: {0}", fileName));
                }

                // create sub directories
                var directories = Directory.GetDirectories(sourceDirectoryPath, "*", SearchOption.TopDirectoryOnly);
                foreach (var directory in directories)
                {
                    var directoryName = Path.GetFileName(directory);
                    var destDirectoryPath = System.IO.Path.Combine(targetDirectoryPath, directoryName);

                    if (!Directory.Exists(destDirectoryPath))
                        Directory.CreateDirectory(destDirectoryPath);

                    var targetSubDirectoryPath = Path.Combine(targetDirectoryPath, directoryName);
                    CopyDirectory(directory, targetSubDirectoryPath);
                }
            }
            catch (Exception ex)
            {
                RaiseMessage(string.Format("Error: {0}", ex.Message));
            }
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

    }
}
