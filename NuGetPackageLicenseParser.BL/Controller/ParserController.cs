using Microsoft.Extensions.Logging;
using NuGetPackageLicenseParser.BL.Model;
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
    public class ParserController
    {
        private readonly ILogger _logger;

        private readonly string _pathSaveLicense;

        public FileElements FileElements { get; }

        public DirectoryElements DirectoryElements { get; }

        public ParserController(ILogger logger)
        {
            _logger = logger;
            _pathSaveLicense = ConfigurationManager.AppSettings["path"];
            FileElements = new FileElements();
            DirectoryElements = new DirectoryElements();

            _logger.LogInformation("Приложение запущено.");
        }

        public void ParsingLicense()
        {
            _logger.LogInformation("Подготовка к выкачиванию лицензий.");

            DirectoryElements.DirectoryCurrentProject = new DirectoryInfo(Path.GetFullPath(Environment.CurrentDirectory + "\\..\\..\\..\\..\\..\\"));

            FileElements.ProjectsFilesNugetCashe = DirectoryElements.DirectoryCurrentProject.GetFiles("project.nuget.cache", SearchOption.AllDirectories)
                .Where(p => !p.DirectoryName.Contains("NuGetPackageLicenseParser"));

            FileElements.LinksNuGet = new List<string>();
            FileElements.LinksLicense = new List<string>();
            FileElements.FileName = new List<string>();

            foreach (var item in FileElements.ProjectsFilesNugetCashe)
            {
                FileElements.LinksNuGet.AddRange(ParseToFiles(item, ".nuget"));
            }

            _logger.LogInformation("Начинается выкачивание лицензий.");

            foreach (var item in FileElements.LinksNuGet.Distinct())
            {
                var text = item.Remove(item.Length - 2).Remove(0, 5);
                var directoryfdh = new DirectoryInfo(Path.GetFullPath(text));
                var filefdh = new FileInfo(directoryfdh.FullName);

                DirectoryElements.DirectoryPackages = new DirectoryInfo(filefdh.DirectoryName);

                GetLicenses();
            }

            for (int k = 0; k < FileElements.LinksLicense.Count; k++)
            {
                _logger.LogInformation($"{FileElements.FileName[k].Replace("<id>", "").Replace("</id>", "")}\n");

                File.WriteAllText($"{_pathSaveLicense}{FileElements.FileName[k].Replace("<id>", "").Replace("</id>", "")}.LICENSE",
                    LoadPage($"{FileElements.LinksLicense[k].Replace("<licenseUrl>", "").Replace("</licenseUrl>", "")}"));
            }

            _logger.LogInformation("Выкачивание лицензий выполнено.");
        }

        private IEnumerable<string> ParseToFiles(FileInfo item, string text) => File.ReadAllLines(item.FullName).Where(p => p.Contains(text));

        private void GetLicenses()
        {
            foreach (var itemFile in DirectoryElements.DirectoryPackages.GetFiles())
            {
                if (itemFile.Name.Contains("LICENSE"))
                {
                    _logger.LogInformation($"{Directory.GetParent(itemFile.DirectoryName).Name}\n");

                    File.Copy(itemFile.FullName, Path.GetFullPath($"{_pathSaveLicense}" + Directory.GetParent(itemFile.DirectoryName).Name) + ".LICENSE", true);
                }
                else if (itemFile.Name.Contains(".nuspec"))
                {
                    FileElements.LinksLicense.AddRange(ParseToFiles(itemFile, "licenseUrl"));
                    FileElements.FileName.AddRange(ParseToFiles(itemFile, "<id>"));
                }
            }
        }

        private string LoadPage(string url)
        {
            var result = string.Empty;
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
