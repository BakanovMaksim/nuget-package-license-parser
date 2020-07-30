using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace NuGetPackageLicenseParser.BL
{
    public class Parser
    {
        public static void ParsingLicense()
        {
            var directoryProject = new DirectoryInfo(Path.GetFullPath(Environment.CurrentDirectory + "\\..\\..\\..\\..\\..\\"));

            var allFilesProjects = directoryProject.GetFiles("project.nuget.cache", SearchOption.AllDirectories).Where(p => !p.DirectoryName.Contains("NuGetPackageLicenseParser"));

            var linksNuGet = new List<string>();
            var linksLicense = new List<string>();
            var fileName = new List<string>();

            foreach (var item in allFilesProjects)
                linksNuGet.AddRange(ParseToFiles(item, ".nuget"));

            DirectoryInfo directoryPackage = null;

            foreach (var item in linksNuGet)
            {
                var text = item.Remove(item.Length - 2).Remove(0, 5);
                var directoryfdh = new DirectoryInfo(Path.GetFullPath(text));
                var filefdh = new FileInfo(directoryfdh.FullName);

                directoryPackage = new DirectoryInfo(filefdh.DirectoryName);

                ForeachDirectory(directoryPackage, linksLicense, fileName);
            }

            for (int k = 0; k < linksLicense.Count; k++)
                File.WriteAllText($"{ConfigurationManager.AppSettings["path"]}{fileName[k].Replace("<id>", "").Replace("</id>", "")}",
                    LoadPage($"{linksLicense[k].Replace("<licenseUrl>", "").Replace("</licenseUrl>", "")}"));
        }

        private static IEnumerable<string> ParseToFiles(FileInfo item, string text) => File.ReadAllLines(item.FullName).Where(p => p.Contains(text));

        private static void ForeachDirectory(DirectoryInfo directoryPackage, List<string> linksLicense, List<string> fileName)
        {
            foreach (var itemFile in directoryPackage.GetFiles())
            {
                if (itemFile.Name.Contains("LICENSE")) File.Copy(itemFile.FullName, Path.GetFullPath($"{ConfigurationManager.AppSettings["path"]}" + Directory.GetParent(itemFile.DirectoryName).Name), true);
                else if (itemFile.Name.Contains(".nuspec"))
                {
                    linksLicense.AddRange(ParseToFiles(itemFile, "licenseUrl"));
                    fileName.AddRange(ParseToFiles(itemFile, "<id>"));
                }
            }
        }

        private static string LoadPage(string url)
        {
            var result = "";
            var request = (HttpWebRequest)WebRequest.Create(url);
            var response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var receiveStream = response.GetResponseStream();
                if (receiveStream != null)
                {
                    StreamReader readStream;
                    if (response.CharacterSet == null)
                        readStream = new StreamReader(receiveStream);
                    else
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                    result = readStream.ReadToEnd();
                    readStream.Close();
                }
                response.Close();
            }
            return Regex.Replace(result, "<[^>]+>|&quot;", string.Empty);
        }
    }
}
