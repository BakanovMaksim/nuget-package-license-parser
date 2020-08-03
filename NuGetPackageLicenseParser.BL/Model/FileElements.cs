using System.Collections.Generic;
using System.IO;

namespace NuGetPackageLicenseParser.BL.Model
{
    public class FileElements
    {
        public IEnumerable<FileInfo> ProjectsFilesNugetCashe { get; set; }

        public List<string> LinksNuGet { get; } = new List<string>();

        public List<string> LinksLicense { get; } = new List<string>();

        public List<string> FileName { get; } = new List<string>();
    }
}
