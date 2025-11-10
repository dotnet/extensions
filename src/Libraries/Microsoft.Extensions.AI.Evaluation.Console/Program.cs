// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Console.Commands;
using Microsoft.Extensions.AI.Evaluation.Console.Telemetry;
using Microsoft.Extensions.Logging;

#if DEBUG
using System.Diagnostics;
#endif

namespace Microsoft.Extensions.AI.Evaluation.Console;

internal sealed class Program
{
    private const string ShortName = "aieval";
    private const string Name = "Microsoft.Extensions.AI.Evaluation.Console";
    private const string Banner = $"{Name} ({ShortName}) version {Constants.Version}";

#pragma warning disable EA0014 // Async methods should support cancellation.
    private static async Task<int> Main(string[] args)
#pragma warning restore EA0014
    {
#pragma warning disable CA1303 // Do not pass literals as localized parameters.
        // Use Console.WriteLine directly instead of ILogger to ensure proper formatting.
        System.Console.WriteLine(Banner);
        System.Console.WriteLine();
#pragma warning restore CA1303

        using ILoggerFactory factory =
            LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                }));

        ILogger logger = factory.CreateLogger(ShortName);
        await logger.DisplayTelemetryOptOutMessageIfNeededAsync().ConfigureAwait(false);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task.
        await using var telemetryHelper = new TelemetryHelper(logger);
#pragma warning restore CA2007

        var rootCmd = new RootCommand(Banner);

#if DEBUG
        var debugOpt = new Option<bool>(["--debug"], "Debug on startup") { IsHidden = true };
        rootCmd.AddGlobalOption(debugOpt);
#endif

        var reportCmd = new Command("report", "Generate a report from a result store");

        var pathOpt =
            new Option<DirectoryInfo>(
                ["-p", "--path"],
                "Root path under which the cache and results are stored")
            {
                IsRequired = false
            };

        var endpointOpt =
            new Option<Uri>(
                ["--endpoint"],
                "Endpoint URL under which the cache and results are stored for Azure Data Lake Gen2 storage")
            {
                IsRequired = false
            };

        var openReportOpt =
            new Option<bool>(
                ["--open"],
                getDefaultValue: () => false,
                "Open the report in the default browser")
            {
                IsRequired = false,
            };

        ValidateSymbolResult<CommandResult> requiresPathOrEndpoint = (CommandResult cmd) =>
        {
            bool hasPath = cmd.GetValueForOption(pathOpt) is not null;
            bool hasEndpoint = cmd.GetValueForOption(endpointOpt) is not null;
            if (!(hasPath ^ hasEndpoint))
            {
                cmd.ErrorMessage = $"Either '{pathOpt.Name}' or '{endpointOpt.Name}' must be specified.";
            }
        };

        reportCmd.AddOption(pathOpt);
        reportCmd.AddOption(endpointOpt);
        reportCmd.AddOption(openReportOpt);
        reportCmd.AddValidator(requiresPathOrEndpoint);

        var outputOpt = new Option<FileInfo>(
            ["-o", "--output"],
            "Output filename/path")
        {
            IsRequired = true,
        };
        reportCmd.AddOption(outputOpt);

        var lastNOpt = new Option<int>(["-n"], () => 10, "Number of most recent executions to include in the report.");
        reportCmd.AddOption(lastNOpt);

        var formatOpt =
            new Option<ReportCommand.Format>(
                ["-f", "--format"],
                () => ReportCommand.Format.html,
                "Specify the format for the generated report.");

        reportCmd.AddOption(formatOpt);

        reportCmd.SetHandler(
            (path, endpoint, output, openReport, lastN, format) =>
                new ReportCommand(logger, telemetryHelper)
                    .InvokeAsync(path, endpoint, output, openReport, lastN, format),
            pathOpt,
            endpointOpt,
            outputOpt,
            openReportOpt,
            lastNOpt,
            formatOpt);

        rootCmd.Add(reportCmd);

        // TASK: Support more granular filters such as the specific scenario / iteration / execution whose results must
        // be cleaned up.
        var cleanResultsCmd = new Command("clean-results", "Delete results");
        cleanResultsCmd.AddOption(pathOpt);
        cleanResultsCmd.AddOption(endpointOpt);
        cleanResultsCmd.AddValidator(requiresPathOrEndpoint);

        var lastNOpt2 = new Option<int>(["-n"], () => 0, "Number of most recent executions to preserve.");
        cleanResultsCmd.AddOption(lastNOpt2);

        cleanResultsCmd.SetHandler(
            (path, endpoint, lastN) =>
                new CleanResultsCommand(logger, telemetryHelper).InvokeAsync(path, endpoint, lastN),
            pathOpt,
            endpointOpt,
            lastNOpt2);

        rootCmd.Add(cleanResultsCmd);

        var cleanCacheCmd = new Command("clean-cache", "Delete expired cache entries");
        cleanCacheCmd.AddOption(pathOpt);
        cleanCacheCmd.AddOption(endpointOpt);
        cleanCacheCmd.AddValidator(requiresPathOrEndpoint);

        cleanCacheCmd.SetHandler(
            (path, endpoint) => new CleanCacheCommand(logger, telemetryHelper).InvokeAsync(path, endpoint),
            pathOpt, endpointOpt);

        rootCmd.Add(cleanCacheCmd);

        // TASK: Support some mechanism to fail a build (i.e. return a failure exit code) based on one or more user
        // specified criteria (e.g., if x% of metrics were deemed 'poor'). Ideally this mechanism would be flexible /
        // extensible enough to allow users to configure multiple different kinds of failure criteria.
        // See https://github.com/dotnet/extensions/issues/6038.
#if DEBUG
        ParseResult parseResult = rootCmd.Parse(args);
        if (parseResult.HasOption(debugOpt))
        {
            Debugger.Launch();
        }
#endif

        int exitCode = await rootCmd.InvokeAsync(args).ConfigureAwait(false);
        return exitCode;
    }
}
