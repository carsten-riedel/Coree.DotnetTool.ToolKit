using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Coree.NETStandard.Services;
using Coree.NETStandard.SpectreConsole;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog.Core;
using Serilog.Events;

using Spectre.Console.Cli;

namespace Coree.DotnetTool.ToolKit.Command
{
    public class SelfUpdateAsyncCommand : AsyncCommand<SelfUpdateAsyncCommand.NugetDelistSettings>
    {
        public class NugetDelistSettings : CommandSettings
        {
            [Description("Minimum loglevel, valid values => Verbose,Debug,Information,Warning,Error,Fatal")]
            [DefaultValue(LogEventLevel.Information)]
            [CommandOption("-l|--loglevel")]
            public LogEventLevel LogEventLevel { get; init; }

            [Description("Throws and errorcode if command is not found.")]
            [DefaultValue(false)]
            [CommandOption("-t|--throwError")]
            public bool ThrowError { get; init; }
        }

        private readonly ILogger<SelfUpdateAsyncCommand> logger;
        private readonly LoggingLevelSwitch loggingLevelSwitch;
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly IProcessService processService;

        public SelfUpdateAsyncCommand(ILogger<SelfUpdateAsyncCommand> logger, LoggingLevelSwitch loggingLevelSwitch, IHostApplicationLifetime hostApplicationLifetime, IProcessService processService)
        {
            this.logger = logger;
            this.loggingLevelSwitch = loggingLevelSwitch;
            this.hostApplicationLifetime = hostApplicationLifetime;
            this.processService = processService;
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
                var result = await processService.RunProcessWithCancellationSupportAsync("dotnet", "tool update Coree.DotnetTool.ToolKit --global --prerelease", "", true, cancellationToken, TimeSpan.FromMinutes(1));

                if (result.ExitCodeState.HasFlag(ProcessRunExitCodeState.IsValidSuccess))
                {
                    logger.LogInformation(result.Output);
                    return (int)SpectreConsoleHostedService.ExitCode.SuccessAndExit;
                }

                if (result.ExitCodeState.HasFlag(ProcessRunExitCodeState.IsFailedStart))
                {
                    logger.LogError("Failed to start the command '{CommandName}'. Ensure the command is installed and accessible.", result.Filename);
                }

                if (result.ExitCodeState.HasFlag(ProcessRunExitCodeState.IsCanceledSet))
                {
                    logger.LogWarning("Command execution '{CommandLine}' was canceled.", result.Commandline);
                }

                if (result.ExitCodeState.HasFlag(ProcessRunExitCodeState.IsValidErrorCode))
                {
                    if ((settings.LogEventLevel == LogEventLevel.Verbose) || (settings.LogEventLevel == LogEventLevel.Debug))
                    {
                        logger.LogError("Command '{CommandName}' exited with error code {ExitCode}.", result.Commandline, result.ExitCode);
                    }
                    else
                    {
                        logger.LogError("Command '{CommandName}' exited with error code {ExitCode}. Change --loglevel to 'Verbose' or 'Debug' for more details.", result.Commandline, result.ExitCode);
                    }
                }

                return settings.ThrowError ? result.ExitCode : 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred during command execution. Review the exception details for more information.");
                return -99;
            }
        }
    }
}