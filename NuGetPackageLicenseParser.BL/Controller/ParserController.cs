using HtmlAgilityPack;

using Microsoft.Extensions.Logging;

using NuGetPackageLicenseParser.BL.Model;

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
        private readonly ILogger logger;

        private readonly string pathCurrentDirectory;

        private readonly string pathSaveLicense;

        public FileElements FileElements { get; }

        public DirectoryElements DirectoryElements { get; }

        public ParserController(ILogger logger, IReadOnlyList<string> arguments)
        {
            if (arguments.Count > 0)
            {
                pathCurrentDirectory = arguments[0];
                pathSaveLicense = arguments[1];
            }

            this.logger = logger;
            pathCurrentDirectory = ConfigurationManager.AppSettings["pathCurrentDirectory"];
            pathSaveLicense = ConfigurationManager.AppSettings["pathSaveLicense"];
            FileElements = new FileElements();
            DirectoryElements = new DirectoryElements();

            this.logger.LogInformation("The app is running.");
        }

        public void ParsingLicenseAsync()
        {
            logger.LogInformation("Preparing to download licenses.");

            GetProjectsFilesNugetCashe();

            ParseFiles();

            logger.LogInformation("License siphoning begins.");

            GetLicenses();

            logger.LogInformation("License saving starts.");

            WriteLicensesSite();

            logger.LogInformation("License extraction completed.");
        }

        private void GetProjectsFilesNugetCashe()
        {
            var directoryCurrentProject = new DirectoryInfo(Path.GetFullPath(pathCurrentDirectory));

            FileElements.ProjectsFilesNugetCashe = directoryCurrentProject.GetFiles("project.nuget.cache", SearchOption.AllDirectories)
                .Where(p => !p.DirectoryName.Contains("NuGetPackageLicenseParser"));
        }

        private void ParseFiles()
        {
            var tasks = new List<Task<IEnumerable<string>>>(FileElements.ProjectsFilesNugetCashe.Count());
            foreach (var item in FileElements.ProjectsFilesNugetCashe)
            {
                tasks.Add(ParseToFiles(item, ".nuget"));
            }

            Task.WaitAll(tasks.ToArray());

            foreach (var t in tasks)
            {
                FileElements.LinksNuGet.AddRange(t.GetAwaiter().GetResult());
            }

            FileElements.LinksNuGet = FileElements.LinksNuGet.Distinct().ToList();
        }

        private void GetLicenses()
        {
            var tasks = new List<Task>();

            foreach (var item in FileElements.LinksNuGet.Where(r => r != null))
            {
                tasks.Add(GetLicensesAsync(item));
            }

            Task.WaitAll(tasks.ToArray());

            FileElements.LinksLicense = FileElements.LinksLicense.Distinct().ToList();
            FileElements.FileName = FileElements.FileName.Distinct().ToList();
        }

        private async Task<IEnumerable<string>> ParseToFiles(FileInfo item, string text) =>
            (await File.ReadAllLinesAsync(item.FullName))
            .Where(p => p.Contains(text))
            .Select(r => r.Trim());

        private DirectoryInfo GetDirectoryPackages(string item)
        {
            var text = item.TrimEnd(',').Trim('"');

            return new DirectoryInfo(Directory.GetParent(text).FullName);
        }

        private async Task GetLicensesAsync(string itemPath)
        {
            var directoryPackages = GetDirectoryPackages(itemPath);

            foreach (var item in directoryPackages.GetFiles())
            {
                if (item.Name.Contains("LICENSE"))
                {
                    logger.LogInformation($"{Directory.GetParent(item.DirectoryName).Name}\n");

                    File.Copy(item.FullName, Path.GetFullPath($"{pathSaveLicense}" + Directory.GetParent(item.DirectoryName).Name) + ".LICENSE.LICENSE", true);
                }
                else if (item.Name.Contains(".nuspec"))
                {
                    FileElements.LinksLicense.AddRange(await ParseToFiles(item, "licenseUrl"));
                    FileElements.FileName.AddRange(await ParseToFiles(item, "<id>"));
                }
            }
        }

        private void WriteLicensesSite()
        {
            var tasks = new List<Task>();

            for (var k = 0; k < FileElements.LinksLicense.Count; k++)
            {
                tasks.Add(WriteLicensesSiteAsync(k));
            }

            Task.WaitAll(tasks.ToArray());
        }

        private async Task WriteLicensesSiteAsync(int count)
        {
            if (FileElements.FileName[count] == null) return;
            if (FileElements.LinksLicense[count] == null) return;

            var file = $"{FileElements.FileName[count].Replace("<id>", string.Empty).Replace("</id>", string.Empty).Trim()}";
            var fileName = $"{pathSaveLicense}{file}.LICENSE";
            var fileContent = await LoadPageAsync($"{FileElements.LinksLicense[count].Replace("<licenseUrl>", string.Empty).Replace("</licenseUrl>", string.Empty)}");

            logger.LogInformation($"{file}\n");

            if (fileContent != null)
                await File.WriteAllTextAsync(fileName, fileContent);
        }

        private async Task<string> LoadPageAsync(string url)
        {
            try
            {
                using var client = new HttpClient();
                using var response = await client.GetAsync(url);
                using var content = response.Content;
                var data = await content.ReadAsStringAsync();
                var result = ParseContentPage(data);

                return result;
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

