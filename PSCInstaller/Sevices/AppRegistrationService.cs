using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Management.Deployment;
namespace PSCInstaller.Services
{
    public class AppRegistrationService 
        : IProgress<DeploymentProgress>
    {

        #region Fields
        Windows.Management.Deployment.PackageManager _packageManager;
        #endregion

        #region Singleton

        public static readonly AppRegistrationService Instance = new AppRegistrationService();

        private AppRegistrationService()
        {
            _packageManager = new Windows.Management.Deployment.PackageManager();
        }

        #endregion

        #region Events
        public event EventHandler<MessageNotificationEventArgs> MessagingEvent;
        private void RaiseMessage(string msg)
        {
            var handler = MessagingEvent;
            if (handler != null)
                handler(this, new MessageNotificationEventArgs(msg));
        }

        public event EventHandler<ProgressUpdateEventArgs> ProgressEvent;
        public void Report(DeploymentProgress progress)
        {
            var handler = ProgressEvent;
            if (handler != null)
                handler(this, new ProgressUpdateEventArgs(progress));
        }

        public event EventHandler<bool> CompletionEvent;
        public void RaiseCompletion(bool success)
        {
            var handler = CompletionEvent;
            if (handler != null)
                handler(this, success);
        }

        #endregion

        public bool InstallCert(string cerPath)
        {
            Uri cerPathUri = DownloadPackageToLocalTempFile(new Uri(cerPath));

            X509Certificate2 cert = new X509Certificate2(cerPathUri.AbsolutePath);
            return InstallCert(cert);
        }

        public bool InstallCert(X509Certificate2 cert)
        {
            RaiseMessage("Installing Certificate: " + cert.FriendlyName);
            try
            {
                X509Store store = new X509Store(StoreName.TrustedPeople, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
                store.Close();
                RaiseMessage("Certificate deployed.");
                return true;
            }
            catch (Exception ex)
            {
                RaiseMessage("Certificate deployment failed: " + ex.Message);
                return false;
            }
        }

        public async Task AddPackageAsync(string packageUri, IEnumerable<Uri> dependencyUris)
        {
            await Task.Yield();
            
            var dependenciesRequiringInstallation = NormalizeDependencies(dependencyUris);
            try
            {
                Uri localPackageUri = new Uri(packageUri);

                localPackageUri = DownloadPackageToLocalTempFile(localPackageUri);

                var deploymentOperation = _packageManager.AddPackageAsync(  localPackageUri,
                                                                            dependenciesRequiringInstallation, 
                                                                            Windows.Management.Deployment.DeploymentOptions.None);
                RaiseMessage(string.Format("Installing package {0}", localPackageUri));
                HandleDeploymentOperation(deploymentOperation);
            }
            catch (Exception ex)
            {
                RaiseMessage(string.Format("Installing package failed, error message: {0}", ex.Message));
                RaiseMessage(string.Format("Full Stacktrace: {0}", ex.ToString()));
            }
        }

        private IEnumerable<Uri> NormalizeDependencies(IEnumerable<Uri> dependencyUris)
        {
            List<Uri> result = new List<Uri>();
            var deviceCPUArchitecture = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE").Replace("AMD", "X");
            foreach (var item in dependencyUris)
            {
                var lastSegment = item.Segments.Last();
                var packageName = lastSegment.Remove(lastSegment.LastIndexOf(".")).ToUpper();

                var architecture = Windows.System.ProcessorArchitecture.Unknown;
                if (packageName.IndexOf(".X86") > -1)
                {
                    architecture = Windows.System.ProcessorArchitecture.X86;
                    if (architecture.ToString() != deviceCPUArchitecture)
                        continue;
                    packageName = packageName.Replace(".X86", "");
                }
                else if (packageName.IndexOf(".ARM") > -1)
                {
                    architecture = Windows.System.ProcessorArchitecture.Arm;
                    if (architecture.ToString() != deviceCPUArchitecture)
                        continue;
                    packageName = packageName.Replace(".ARM", "");
                }
                else if (packageName.IndexOf(".X64") > -1)
                {
                    architecture = Windows.System.ProcessorArchitecture.X64;
                    if (architecture.ToString() != deviceCPUArchitecture)
                        continue;
                    packageName = packageName.Replace(".X64", "");
                }

                if (!DoesDependencyPackageExist(packageName, architecture))
                    result.Add(item);
            }
            return result;
        }

        private Uri DownloadPackageToLocalTempFile(Uri packageUri)
        {
            if (!packageUri.IsFile)
            {
                //download to local
                WebClient downloadWebClient = new WebClient();

                string tempFilePath = Path.GetTempFileName();

                RaiseMessage("Downloading File...");
                downloadWebClient.DownloadFile(packageUri, tempFilePath);
                RaiseMessage("File downloaded to " + tempFilePath);

                packageUri = new Uri(tempFilePath);

            }
            return packageUri;
        }

        public async Task UpdatePackageAsync(string inputPackageUri)
        {
            await Task.Yield();

            try
            {
                Uri packageUri = new Uri(inputPackageUri);

                packageUri = DownloadPackageToLocalTempFile(packageUri);

                Windows.Foundation.IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation = _packageManager.UpdatePackageAsync(packageUri, null, Windows.Management.Deployment.DeploymentOptions.None);

                RaiseMessage(string.Format("Updating package {0}", inputPackageUri));

                HandleDeploymentOperation(deploymentOperation);

            }
            catch (Exception ex)
            {
                RaiseMessage(string.Format("Updating package failed, error message: {0}", ex.Message));
                RaiseMessage(string.Format("Full Stacktrace: {0}", ex.ToString()));
            }
        }

        public async Task RemovePackageAsync(string fullPackageName)
        {
            try
            {
                RaiseMessage(string.Format("Removing package {0}", fullPackageName));

                Windows.Foundation.IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation = _packageManager.RemovePackageAsync(fullPackageName);

                HandleDeploymentOperation(deploymentOperation);
            }
            catch (Exception ex)
            {
                RaiseMessage(string.Format("Removing package failed, error message: {0}", ex.Message));
                RaiseMessage(string.Format("Full Stacktrace: {0}", ex.ToString()));
            }
            await Task.Yield();
        }

        public async Task RemovePackageAsync(string inputPackageName, string publisherId)
        {
            var pkg = FindPackage(inputPackageName);
            if (pkg != null)
            {
                await RemovePackageAsync(pkg.AppPackageFullName);
            }
        }
        public bool DoesDependencyPackageExist(string packageName, Windows.System.ProcessorArchitecture architecture)
        {
            bool result = false;
            var packages = _packageManager.FindPackages().ToList();
            Windows.ApplicationModel.Package installedPackage = null;
            foreach (var pkg in packages)
            {
                if (string.Compare(pkg.Id.Name, packageName, true) == 0 && pkg.Id.Architecture == architecture)
                {
                    var fpkg = _packageManager.FindPackages(pkg.Id.Name, pkg.Id.Publisher).ToList();
                    installedPackage = fpkg.FirstOrDefault();
                    break;
                }
            }

            return result = (installedPackage != null);
        }

        public UserAppPackageDataModel FindPackage(string packageName)
        {
            UserAppPackageDataModel result = null;
            try
            {
                var packages = _packageManager.FindPackages().ToList();
                Windows.ApplicationModel.Package installedPackage = null;
                foreach (var pkg in packages)
                {
                    if (string.Compare(pkg.Id.Name, packageName, true) == 0)
                    {
                        var fpkg = _packageManager.FindPackages(pkg.Id.Name, pkg.Id.Publisher).ToList();
                        installedPackage = fpkg.FirstOrDefault();
                        break;
                    }
                }
 
                if (installedPackage == null)
                {
                    RaiseMessage("No packages were found.");
                }
                else
                {
                    RaiseMessage("Package was found.");
                    result = new UserAppPackageDataModel(installedPackage);
                }

            }
            catch (UnauthorizedAccessException)
            {
                RaiseMessage("packageManager.FindPackages() failed because access was denied. This program must be run from an elevated command prompt.");
 
            }
            catch (Exception ex)
            {
                RaiseMessage(string.Format("packageManager.FindPackages() failed, error message: {0}", ex.Message));
                RaiseMessage(string.Format("Full Stacktrace: {0}", ex.ToString()));
            }

            return result;
        }

        private void HandleDeploymentOperation(Windows.Foundation.IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation)
        {
            RaiseMessage("Waiting for the operation to complete...");

            deploymentOperation.Progress = (result, progress) => 
            {
                Report(progress);
            };

            deploymentOperation.Completed = (depProgress, status) => 
            {
                if (deploymentOperation.Status == Windows.Foundation.AsyncStatus.Error)
                {
                    Windows.Management.Deployment.DeploymentResult deploymentResult = deploymentOperation.GetResults();
                    RaiseMessage(string.Format("Operation Error: {0}", deploymentOperation.ErrorCode));
                    RaiseMessage(string.Format("Detailed Error Text: {0}", deploymentResult.ErrorText)); //  0x800B0109
                    Report(new DeploymentProgress() { percentage = 100 });
                }
                else if (deploymentOperation.Status == Windows.Foundation.AsyncStatus.Canceled)
                {
                    RaiseMessage("Operation Canceled");
                }
                else if (deploymentOperation.Status == Windows.Foundation.AsyncStatus.Completed)
                {
                    RaiseMessage("Operation succeeded!");
                }
                else
                {
                    RaiseMessage("Operation status unknown");
                }

                RaiseCompletion(status == Windows.Foundation.AsyncStatus.Completed); 
            };
        }

        public string EnableLoopbackExemptForApp(string pkgFamilyName)
        {
            //CheckNetIsolation.exe LoopbackExempt -a -n=4eee5a62-26ee-4c1f-ab12-64ef8a765cfa_j05ea6na1385w
            var proc = new Process();
            proc.StartInfo.FileName = "CheckNetIsolation.exe";
            proc.StartInfo.Arguments = "LoopbackExempt -a -n=" + pkgFamilyName;
            proc.Start();
            proc.WaitForExit();
            var exitCode = proc.ExitCode;
            proc.Close();
            return exitCode.ToString();
        }
    }
}
