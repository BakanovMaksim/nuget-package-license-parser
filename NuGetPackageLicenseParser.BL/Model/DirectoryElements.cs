using System.IO;

namespace NuGetPackageLicenseParser.BL.Model
{
    public class DirectoryElements
    {
        public DirectoryInfo DirectoryCurrentProject { get; set; }

        public DirectoryInfo DirectoryPackages { get; set; }
    }
}
