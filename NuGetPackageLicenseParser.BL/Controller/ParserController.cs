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

            var task = Task.Factory.StartNew(GetProjectsFilesNugetCashe);

            task.Wait();

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

            Parallel.For(0, FileElements.LinksLicense.Count - 1, WriteLicensesSiteAsync);

            _logger.LogInformation("Выкачивание лицензий выполнено.");
        }

        private void GetProjectsFilesNugetCashe()
        {
            DirectoryElements.DirectoryCurrentProject = new DirectoryInfo(Path.GetFullPath(Environment.CurrentDirectory + "\\..\\..\\..\\..\\..\\"));

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

                    File.Copy(item.FullName, Path.GetFullPath($"{_pathSaveLicense}" + Directory.GetParent(item.DirectoryName).Name) + ".LICENSE", true);
                }
                else if (item.Name.Contains(".nuspec"))
                {
                    FileElements.LinksLicense.AddRange(ParseToFiles(item, "licenseUrl"));
                    FileElements.FileName.AddRange(ParseToFiles(item, "<id>"));
                }
            }
        }

        private async void WriteLicensesSiteAsync(int count)
        {
            _logger.LogInformation($"{FileElements.FileName[count].Replace("<id>", "").Replace("</id>", "")}\n");

            var qwe = $"{_pathSaveLicense}{FileElements.FileName[count].Replace("<id>", "").Replace("</id>", "")}.LICENSE";
            var asd = await LoadPageAsync($"{FileElements.LinksLicense[count].Replace("<licenseUrl>", "").Replace("</licenseUrl>", "")}");
            await File.WriteAllTextAsync(qwe, asd);
        }

        private async Task<string> LoadPageAsync(string url)
        {
            var client = new HttpClient();
            var response = await client.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            return Regex.Replace(result, "<[^>]+>|&quot;", string.Empty);
        }
    }
}

