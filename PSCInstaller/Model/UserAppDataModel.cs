using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSCInstaller
{
    public class UserAppPackageDataModel
    {
       // public string Version { get; set; }
        
        //packageid.name
        public string AppPackageName { get; set; }
        public string AppPublisherName { get; set; }

        public string AppPackageFamilyName { get; set; }

        public string AppPackageFullName { get; set; }

        public string AppPackageVersion { get; set; }              
        
        public UserAppPackageDataModel(Windows.ApplicationModel.Package package)
        {
            this.AppPackageName = package.Id.Name;
            this.AppPackageFullName = package.Id.FullName;
            this.AppPackageFamilyName = package.Id.FamilyName;
            this.AppPublisherName = package.Id.Publisher;
            this.AppPackageVersion = string.Format("{0}.{1}.{2}.{3}", package.Id.Version.Major, package.Id.Version.Minor,
                package.Id.Version.Build, package.Id.Version.Revision);

        }

        private void DisplayPackageInfo(Windows.ApplicationModel.Package package)
        {
            Console.WriteLine("Name: {0}", package.Id.Name);

            Console.WriteLine("FullName: {0}", package.Id.FullName);

            Console.WriteLine("Version: {0}.{1}.{2}.{3}", package.Id.Version.Major, package.Id.Version.Minor,
                package.Id.Version.Build, package.Id.Version.Revision);

            Console.WriteLine("Publisher: {0}", package.Id.Publisher);

            Console.WriteLine("PublisherId: {0}", package.Id.PublisherId);

            Console.WriteLine("Installed Location: {0}", package.InstalledLocation.Path);

            //        Console.WriteLine("Architecture: {0}",
            //            Enum.GetName(typeof(Windows.Management.Deployment.PackageArchitecture), package.Id.Architecture));

            Console.WriteLine("IsFramework: {0}", package.IsFramework);
        }

    }
}
