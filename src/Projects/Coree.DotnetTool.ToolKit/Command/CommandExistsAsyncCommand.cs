using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Coree.NETStandard.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace Coree.DotnetTool.ToolKit.Command
{
    public class CommandExistsAsyncCommand : AsyncCommand<CommandExistsAsyncCommand.CommandExistsSettings>
    {
        public sealed class CommandExistsSettings : CommandSettings
        {
            [Description("")]
            [CommandArgument(0, "<CommandName>")]
            public string? CommandName { get; init; }

            [Description("")]
            [DefaultValue(false)]
            [CommandOption("-t|--throwError")]
            public bool ThrowError { get; init; }

        }

        private readonly ILogger<CommandExistsAsyncCommand> logger;
        private readonly IFileService fileService;

        public CommandExistsAsyncCommand(ILogger<CommandExistsAsyncCommand> logger,IFileService fileService)
        {
            this.logger = logger;
            this.fileService = fileService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, CommandExistsSettings settings)
        {
            Program.ExitCode = await ExecuteCancelAsync(Program.cancellationTokenSource.Token, settings);
            return Program.ExitCode;
        }

        private async Task<int> ExecuteCancelAsync(CancellationToken cancellationToken, CommandExistsSettings settings)
        {
            try
            {
                var foundat = fileService.IsCommandAvailable(settings.CommandName);
                if (settings.ThrowError && foundat ==null)
                {
                    logger.LogError("Command {CommandName} not found. Throwing error exitcode.", settings.CommandName);
                    return -99;
                } else if (foundat == null)
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

            return 0;
            // Cleanup or final actions can also be placed here if they should be executed regardless of cancellation
        }
    }
}
