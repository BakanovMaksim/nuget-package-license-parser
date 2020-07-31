using Microsoft.Extensions.Logging;
using NuGetPackageLicenseParser.BL;

namespace NuGetPackageLicenseParser.UI
{
    class Program
    {
        static void Main(string[] args)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ILogger logger = loggerFactory.CreateLogger<ParserController>();

            ParserController parseController = new ParserController(logger);
            parseController.ParsingLicense();
        }
    }
}
