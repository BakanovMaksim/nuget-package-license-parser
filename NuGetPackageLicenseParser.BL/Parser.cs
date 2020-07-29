using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace NuGetPackageLicenseParser.BL
{
    public class Parser
    {
        public static void ParsingLicense()
        {
            var directoryProject = new DirectoryInfo(Path.GetFullPath(Environment.CurrentDirectory + "\\..\\..\\..\\..\\..\\"));

            var allFilesProjects = directoryProject.GetFiles("project.nuget.cache", SearchOption.AllDirectories).Where(p => !p.DirectoryName.Contains("NuGetPackageLicenseParser"));

            var linksNuGet = new List<string>();

            foreach (var item in allFilesProjects)
            {
                var file = File.ReadAllLines(item.FullName);
                var lin = file.Where(p => p.Contains(".nuget")).Select(p => p); 
                linksNuGet.AddRange(lin); 
            }

            DirectoryInfo directoryPackage = null;

            foreach(var item in linksNuGet)
            {
                var text = item.Remove(item.Length - 2).Remove(0, 5);
                var directoryfdh = new DirectoryInfo(Path.GetFullPath(text));
                var file = new FileInfo(directoryfdh.FullName); 

                directoryPackage = new DirectoryInfo(file.DirectoryName);

                foreach(var itemFile in directoryPackage.GetFiles())
                {
                    if (itemFile.Name.Contains(".nuspec")) Console.WriteLine(itemFile.Name);
                }
            }
        }
    }
}
