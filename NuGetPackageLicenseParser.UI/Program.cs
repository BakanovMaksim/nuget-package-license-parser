using Microsoft.Extensions.Logging;
using NuGetPackageLicenseParser.BL;
using System;
using System.Threading.Tasks;

namespace NuGetPackageLicenseParser.UI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ILogger logger = loggerFactory.CreateLogger<ParserController>();

            ParserController parseController = new ParserController(logger, args);
            await parseController.ParsingLicenseAsync();
        }
    }
}
