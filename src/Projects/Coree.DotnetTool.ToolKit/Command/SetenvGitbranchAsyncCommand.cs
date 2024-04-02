using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Coree.NETStandard.Extensions.Strings;
using Coree.NETStandard.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

using Spectre.Console.Cli;

namespace Coree.DotnetTool.ToolKit.Command
{
    public class SetenvGitbranchAsyncCommand : AsyncCommand<SetenvGitbranchAsyncCommand.SetenvGitrootSettings>
    {
        public sealed class SetenvGitrootSettings : CommandSettings
        {
            [Description("Minimum loglevel, valid values => Verbose,Debug,Information,Warning,Error,Fatal")]
            [DefaultValue(LogEventLevel.Information)]
            [CommandOption("-l|--loglevel")]
            public LogEventLevel LogEventLevel { get; init; }

            [Description("Throws and errorcode if unsuccessfull")]
            [DefaultValue(false)]
            [CommandOption("-t|--throwError")]
            public bool ThrowError { get; set; }


            [Description("Writes the output as plain text, that can be used to set an enviromentvariable \"toolkit -g setenv-gitbranch >> $GITHUB_ENV \" ")]
            [DefaultValue(false)]
            [CommandOption("-g|--githubaction")]
            public bool GitHubAction { get; init; }
        }

        private readonly ILogger<SetenvGitrootAsyncCommand> logger;
        private readonly LoggingLevelSwitch loggingLevelSwitch;
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly IProcessService processService;

        public SetenvGitbranchAsyncCommand(ILogger<SetenvGitrootAsyncCommand> logger, LoggingLevelSwitch loggingLevelSwitch,IHostApplicationLifetime hostApplicationLifetime, IProcessService processService)
        {
            this.logger = logger;
            this.loggingLevelSwitch = loggingLevelSwitch;
            this.hostApplicationLifetime = hostApplicationLifetime;
            this.processService = processService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, SetenvGitrootSettings settings)
        {
            loggingLevelSwitch.MinimumLevel = settings.LogEventLevel;
            if (settings.GitHubAction)
            {
                loggingLevelSwitch.MinimumLevel = LogEventLevel.Fatal;
                settings.ThrowError = true;
            }
            return await ExecuteCancelAsync(settings, hostApplicationLifetime.ApplicationStopping);
        }

        private async Task<int> ExecuteCancelAsync(SetenvGitrootSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var result = await processService.RunProcessWithCancellationSupportAsync("git", "rev-parse --abbrev-ref HEAD", "", true, cancellationToken, TimeSpan.FromMinutes(1));

                if (result.ExitCodeState.HasFlag(ProcessRunExitCodeState.IsValidSuccess))
                {
                    var firstResult = result.Output.SplitWith(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    logger.LogInformation("Outputs first line result is {output}.", firstResult);
                    Console.Write(@$"TOOLKIT_GITBRANCH={firstResult}{Environment.NewLine}");
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
