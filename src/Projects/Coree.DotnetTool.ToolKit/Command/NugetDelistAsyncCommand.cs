using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Coree.NETStandard.Classes.HttpRequestService;
using Coree.NETStandard.Extensions.Conversions.String;
using Coree.NETStandard.SpectreConsole;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog.Core;
using Serilog.Events;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Coree.DotnetTool.ToolKit.Command
{
    public class NugetDelistAsyncCommand : AsyncCommand<NugetDelistAsyncCommand.NugetDelistSettings>
    {
        public class NugetDelistSettings : CommandSettings
        {
            [Description("The shell commandline command e.g. cmd,curl,bash")]
            [CommandArgument(0, "<PackageName>")]
            public string? PackageName { get; set; }

            [Description("Minimum loglevel, valid values => Verbose,Debug,Information,Warning,Error,Fatal")]
            [DefaultValue(LogEventLevel.Information)]
            [CommandOption("-l|--loglevel")]
            public LogEventLevel LogEventLevel { get; init; }

            [Description("Throws and errorcode if command is not found.")]
            [DefaultValue(false)]
            [CommandOption("-t|--throwError")]
            public bool ThrowError { get; init; }

            public override ValidationResult Validate()
            {

                if (String.IsNullOrWhiteSpace(PackageName))
                {
                    return ValidationResult.Error("Required argument <PackageName> cannot be empty");
                }

                return ValidationResult.Success();
            }
        }

        private readonly ILogger<NugetDelistAsyncCommand> logger;
        private readonly LoggingLevelSwitch loggingLevelSwitch;
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly HttpRequestService httpRequestService;

        public NugetDelistAsyncCommand(ILogger<NugetDelistAsyncCommand> logger, LoggingLevelSwitch loggingLevelSwitch, IHostApplicationLifetime hostApplicationLifetime, HttpRequestService httpRequestService)
        {
            this.logger = logger;
            this.loggingLevelSwitch = loggingLevelSwitch;
            this.hostApplicationLifetime = hostApplicationLifetime;
            this.httpRequestService = httpRequestService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, NugetDelistSettings settings)
        {
            loggingLevelSwitch.MinimumLevel = settings.LogEventLevel;
            return await ExecuteCancelAsync(settings, hostApplicationLifetime.ApplicationStopping);
        }

        private async Task<int> ExecuteCancelAsync(NugetDelistSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var serviceIndex = await httpRequestService.CreateRequest(HttpMethod.Get, new Uri("https://api.nuget.org/v3/index.json"), cacheDuration: TimeSpan.FromHours(24 * 30), cancellationToken: cancellationToken);
                var registrationsBaseUrls = serviceIndex.ResponseString?.ToJsonPathResultValues<string>("$.resources[?(@['@type'] == 'RegistrationsBaseUrl/3.6.0')]..['@id']");
                var registrationsBaseUrl = registrationsBaseUrls?.FirstOrDefault();
                var packageIndexUrl = registrationsBaseUrl == null ? null : $"{registrationsBaseUrl}{settings.PackageName!.ToLowerInvariant()}/index.json";

                string? packageIndexJson = null;
                if (!String.IsNullOrWhiteSpace(packageIndexUrl))
                {
                    var packageIndex = await httpRequestService.CreateRequest(HttpMethod.Get, new Uri(packageIndexUrl), cancellationToken: cancellationToken);
                    packageIndexJson = packageIndex.ResponseString;
                }

                var listedPackages = packageIndexJson?.ToJsonPathResultValues<string>("$.items[*].items[*][?(@.listed == true)].version");
                var unlistedPackages = packageIndexJson?.ToJsonPathResultValues<string>("$.items[*].items[*][?(@.listed == false)].version");

                if (listedPackages == null || unlistedPackages == null)
                {
                    logger.LogError("Package {PackageName} not found. Throwing error exitcode.", settings.PackageName);
                    return -99;
                }

                foreach (var item in listedPackages)
                {
                    logger.LogInformation("{item} is listed", item.PadRight(15));
                }

                foreach (var item in unlistedPackages)
                {
                    logger.LogInformation("{item} is unlisted", item.PadRight(15));
                }

                return (int)SpectreConsoleHostedService.ExitCode.SuccessAndExit;

            }
            catch (OperationCanceledException ex)
            {
                logger.LogError(ex, "OperationCanceledException");
                return (int)SpectreConsoleHostedService.ExitCode.CommandTerminated;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception");
                return (int)SpectreConsoleHostedService.ExitCode.CommandFailedToRun;
            }
        }
    }
}
