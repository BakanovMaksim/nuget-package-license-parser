using HtmlAgilityPack;
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
using System.Threading.Tasks;

namespace NuGetPackageLicenseParser.BL
{
    public class ParserController
    {
        private readonly ILogger _logger;

        private readonly string _pathCurrentDirectory;

        private readonly string _pathSaveLicense;

        public FileElements FileElements { get; }

        public DirectoryElements DirectoryElements { get; }

        public ParserController(ILogger logger)
        {
            _logger = logger;
            _pathCurrentDirectory = ConfigurationManager.AppSettings["pathCurrentDirectory"];
            _pathSaveLicense = ConfigurationManager.AppSettings["pathSaveLicense"];
            FileElements = new FileElements();
            DirectoryElements = new DirectoryElements();

            _logger.LogInformation("Приложение запущено.");
        }

        public void ParsingLicense()
        {
            _logger.LogInformation("Подготовка к выкачиванию лицензий.");

            GetProjectsFilesNugetCashe();

            var forEachFiles = Parallel.ForEach(FileElements.ProjectsFilesNugetCashe, item =>
            {
                FileElements.LinksNuGet.AddRange(ParseToFiles(item, ".nuget"));
            });

            _logger.LogInformation("Начинается выкачивание лицензий.");

            if (forEachFiles.IsCompleted)
            {
                Parallel.ForEach(FileElements.LinksNuGet.Distinct(), item =>
                {
                    DirectoryElements.DirectoryPackages = GetDirectoryPackages(item);

                    GetLicenses();
                });
            }

            Parallel.For(0, FileElements.LinksLicense.Count - 1, WriteLicensesSite);

            _logger.LogInformation("Выкачивание лицензий выполнено.");
        }

        private void GetProjectsFilesNugetCashe()
        {
            DirectoryElements.DirectoryCurrentProject = new DirectoryInfo(Path.GetFullPath(_pathCurrentDirectory));

            FileElements.ProjectsFilesNugetCashe = DirectoryElements.DirectoryCurrentProject.GetFiles("project.nuget.cache", SearchOption.AllDirectories)
                .Where(p => !p.DirectoryName.Contains("NuGetPackageLicenseParser"));
        }

        private IEnumerable<string> ParseToFiles(FileInfo item, string text) => File.ReadAllLines(item.FullName).Where(p => p.Contains(text));

        private DirectoryInfo GetDirectoryPackages(string item)
        {
            var text = item.Remove(item.Length - 2).Remove(0, 5);

            return new DirectoryInfo(Directory.GetParent(text).FullName);
        }

        private void GetLicenses()
        {
            foreach (var item in DirectoryElements.DirectoryPackages.GetFiles())
            {
                if (item.Name.Contains("LICENSE"))
                {
                    _logger.LogInformation($"{Directory.GetParent(item.DirectoryName).Name}\n");

                    File.Copy(item.FullName, Path.GetFullPath($"{_pathSaveLicense}" + Directory.GetParent(item.DirectoryName).Name) + ".LICENSE.LICENSE", true);
                }
                else if (item.Name.Contains(".nuspec"))
                {
                    FileElements.LinksLicense.AddRange(ParseToFiles(item, "licenseUrl"));
                    FileElements.FileName.AddRange(ParseToFiles(item, "<id>"));
                }
            }
        }

        private void WriteLicensesSite(int count)
        {
            _logger.LogInformation($"{FileElements.FileName[count].Replace("<id>", string.Empty).Replace("</id>", string.Empty)}\n");

            var fileName = $"{_pathSaveLicense}{FileElements.FileName[count].Replace("<id>", string.Empty).Replace("</id>", string.Empty)}.LICENSE.LICENSE";
            var fileContent = LoadPage($"{FileElements.LinksLicense[count].Replace("<licenseUrl>", string.Empty).Replace("</licenseUrl>", string.Empty)}");

            if(fileContent != null) 
                File.WriteAllText(fileName, fileContent);
        }

        private string LoadPage(string url)
        {
            var data = string.Empty;
            
            try
            {
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
                        data = readStream.ReadToEnd();
                        readStream.Close();
                    }
                    response.Close();
                }
            }
            catch(WebException)
            {
                return null;
            }

            var result = ParseContentPage(data);

            return result;
        }

        private string ParseContentPage(string data)
        {
            var document = new HtmlDocument();
            document.LoadHtml(data);
            var result = document.DocumentNode.InnerText;

            return Regex.Replace(result, "&quot;|&nbsp;|&#39;", string.Empty);
        }
    }
}

