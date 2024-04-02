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
using Spectre.Console;
using Spectre.Console.Cli;

namespace Coree.DotnetTool.ToolKit.Command
{
    public class CommandExistsAsyncCommand : AsyncCommand<CommandExistsAsyncCommand.CommandExistsSettings>
    {
        public class CommandExistsSettings : CommandSettings
        {
            [Description("The shell commandline command e.g. cmd,curl,bash")]
            [CommandArgument(0, "<CommandName>")]
            public string? CommandName { get; init; }

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

                if (String.IsNullOrWhiteSpace(CommandName))
                {
                    return ValidationResult.Error("Required argument <CommandName> cannot be empty");
                }

                return ValidationResult.Success();
            }
        }

        private readonly ILogger<CommandExistsAsyncCommand> logger;
        private readonly LoggingLevelSwitch loggingLevelSwitch;
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly IFileService fileService;

        public CommandExistsAsyncCommand(ILogger<CommandExistsAsyncCommand> logger, LoggingLevelSwitch loggingLevelSwitch, IHostApplicationLifetime hostApplicationLifetime,IFileService fileService)
        {
            this.logger = logger;
            this.loggingLevelSwitch = loggingLevelSwitch;
            this.hostApplicationLifetime = hostApplicationLifetime;
            this.fileService = fileService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, CommandExistsSettings settings)
        {
            loggingLevelSwitch.MinimumLevel = settings.LogEventLevel;
            return await ExecuteCancelAsync(settings, hostApplicationLifetime.ApplicationStopping);
        }

        private async Task<int> ExecuteCancelAsync(CommandExistsSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var foundat = await fileService.IsCommandAvailableAsync(settings.CommandName, cancellationToken);
                if (settings.ThrowError && foundat == null)
                {
                    logger.LogError("Command {CommandName} not found. Throwing error exitcode.", settings.CommandName);
                    return -99;
                }
                else if (foundat == null)
                {
                    logger.LogError("Command {CommandName} not found.", settings.CommandName);
                    return (int)SpectreConsoleHostedService.ExitCode.SuccessAndExit;
                }
                else
                {
                    logger.LogInformation("Command {CommandName} found in {location}", System.IO.Path.GetFileName(foundat), foundat);
                    return (int)SpectreConsoleHostedService.ExitCode.SuccessAndExit;
                }
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