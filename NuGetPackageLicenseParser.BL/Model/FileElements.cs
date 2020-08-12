using System.Collections.Generic;
using System.IO;

namespace NuGetPackageLicenseParser.BL.Model
{
    public class FileElements
    {
        public IEnumerable<FileInfo> ProjectsFilesNugetCashe { get; set; }

        public List<string> LinksNuGet { get; set; } = new List<string>();

        public List<string> LinksLicense { get; set; } = new List<string> ();

        public List<string> FileName { get; set; } = new List<string>();
    }
}
