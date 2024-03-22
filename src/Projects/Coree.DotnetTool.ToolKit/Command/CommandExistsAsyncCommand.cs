using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Coree.NETStandard.Services;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Coree.DotnetTool.ToolKit.Command
{
    public class CommandExistsAsyncCommand : AsyncCommand<CommandExistsAsyncCommand.CommandExistsSettings>
    {
        public sealed class CommandExistsSettings : CommandSettings
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
        private readonly IFileService fileService;
        private readonly LoggingLevelSwitch loggingLevelSwitch;

        public CommandExistsAsyncCommand(ILogger<CommandExistsAsyncCommand> logger, LoggingLevelSwitch loggingLevelSwitch, Coree.NETStandard.Services.IFileService fileService)
        {
            this.logger = logger;
            this.fileService = fileService;
            this.loggingLevelSwitch = loggingLevelSwitch;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, CommandExistsSettings settings)
        {
            loggingLevelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
            return await ExecuteCancelAsync(settings, Program.cancellationTokenSource.Token);
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
                    return 0;
                }
                else
                {
                    logger.LogInformation("Command {CommandName} found in {location}", System.IO.Path.GetFileName(foundat), foundat);
                    return 0;
                }
            }
            catch (OperationCanceledException ex)
            {
                logger.LogError(ex, "OperationCanceledException");
                return -99;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception");
                return -2;
            }
        }
    }
}