using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NuGetPackageLicenseParser.BL.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
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

        public ParserController(ILogger logger,IReadOnlyList<string> arguments)
        {
            if(arguments.Count > 0)
            {
                _pathCurrentDirectory = arguments[0];
                _pathSaveLicense = arguments[1];
            }

            _logger = logger;
            _pathCurrentDirectory = ConfigurationManager.AppSettings["pathCurrentDirectory"];
            _pathSaveLicense = ConfigurationManager.AppSettings["pathSaveLicense"];
            FileElements = new FileElements();
            DirectoryElements = new DirectoryElements();

            _logger.LogInformation("Приложение запущено.");
        }

        public async Task ParsingLicenseAsync()
        {
            _logger.LogInformation("Подготовка к выкачиванию лицензий.");

            GetProjectsFilesNugetCashe();

            var forEachFiles = Parallel.ForEach(
                source: FileElements.ProjectsFilesNugetCashe,
                localInit: () => new List<string> { },
                body: (item, state, localvalue) =>
                {
                    localvalue.AddRange(ParseToFiles(item, ".nuget"));
                    return localvalue;
                },
                localFinally: localValue =>
                {
                    FileElements.LinksNuGet.AddRange(localValue);
                });

            _logger.LogInformation("Начинается выкачивание лицензий.");

            if (forEachFiles.IsCompleted)
            {
                foreach (var item in FileElements.LinksNuGet)
                {
                    DirectoryElements.DirectoryPackages = GetDirectoryPackages(item);

                    GetLicenses();
                }
            }

            for (int k = 0; k < FileElements.LinksLicense.Count; k++)
            {
                await WriteLicensesSiteAsync(k);
            }

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

        private async Task WriteLicensesSiteAsync(int count)
        {
            _logger.LogInformation($"{FileElements.FileName[count].Replace("<id>", string.Empty).Replace("</id>", string.Empty)}\n");

            var fileName = $"{_pathSaveLicense}{FileElements.FileName[count].Replace("<id>", string.Empty).Replace("</id>", string.Empty)}.LICENSE.LICENSE";
            var fileContent = await LoadPageAsync($"{FileElements.LinksLicense[count].Replace("<licenseUrl>", string.Empty).Replace("</licenseUrl>", string.Empty)}");

            if (fileContent != null)
                await File.WriteAllTextAsync(fileName, fileContent);
        }

        private async Task<string> LoadPageAsync(string url)
        {
            try
            {
                using (var client = new HttpClient())
                using (var response = await client.GetAsync(url))
                using (var content = response.Content)
                {
                    var data = await content.ReadAsStringAsync();
                    var result = ParseContentPage(data);

                    return result;
                }
            }
            catch (HttpRequestException)
            {
                return null;
            }
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

