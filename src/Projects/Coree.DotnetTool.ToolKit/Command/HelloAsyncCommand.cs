﻿using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Coree.DotnetTool.ToolKit.Command
{
    public class HelloAsyncCommand : AsyncCommand<HelloAsyncCommand.OnBranchCopyDirectorySettings>
    {
        private readonly ILogger<HelloAsyncCommand> logger;

        public sealed class OnBranchCopyDirectorySettings : CommandSettings
        {
            [Description("Specifies the path to the .pac file. Accepts local file paths and URLs (http, https, ftp). Requires direct network access. For Windows, leaving this empty defaults to registry settings. On other operating systems, this argument is mandatory. e.g. https://yourproxy.com/proxy.pac, C:\\config\\proxy.pac, Path\\proxy.pac ")]
            [CommandArgument(0, "<BranchName>")]
            public string? BranchName { get; init; }

            [CommandOption(template: "-s|--source")]
            [Description("Defines the URL prefix for the output of FindProxyForUrl. Acceptable values: 'none', 'http', 'https'. Defaults to 'http'. The format applied is '[[urlprefix]]://[[host]]:[[port]]'.")]
            [DefaultValue("")]
            public string? HostPrefix { get; init; }

            [Description("Specifies the URL for which to find the appropriate proxy settings. This option allows you to determine the proxy configuration for a specific web resource. By default, it is set to 'https://example.com'.")]
            [DefaultValue("")]
            [CommandOption("-d|--destination")]
            public string? HostPrefixd { get; init; }

            public override ValidationResult Validate()
            {

                if (HostPrefix == "Hello")
                {
                    return ValidationResult.Error("Required argument cannot be 'Hello'");
                }

                return ValidationResult.Success();
            }
        }

        public HelloAsyncCommand(ILogger<HelloAsyncCommand> logger, ILoggingService loggingService)
        {
            this.logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, OnBranchCopyDirectorySettings settings)
        {
            Program.ExitCode = await ExecuteCancelAsync(settings,Program.cancellationTokenSource.Token);
            return Program.ExitCode;
        }

        private async Task<int> ExecuteCancelAsync(OnBranchCopyDirectorySettings settings,CancellationToken cancellationToken)
        {
            int x = 0;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Simulate some work by delaying
                    await Task.Delay(1000, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    logger.LogInformation("Working...");
                    x++;
                    if (x > 10)
                    {
                        break;
                    }
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