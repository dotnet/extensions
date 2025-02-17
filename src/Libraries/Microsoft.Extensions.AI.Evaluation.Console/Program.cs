// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DEBUG
using System.CommandLine.Parsing;
using System.Diagnostics;
#endif
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Console.Commands;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.AI.Evaluation.Console;

internal sealed class Program
{
    private const string Name = "Microsoft.Extensions.AI.Evaluation.Console";
    private const string Banner = $"{Name} [{Constants.Version}]";

#pragma warning disable EA0014 // Async methods should support cancellation.
    private static async Task<int> Main(string[] args)
#pragma warning restore EA0014
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger(Name);
        logger.LogInformation("{banner}", Banner);

        var rootCmd = new RootCommand(Banner);

#if DEBUG
        var debugOpt = new Option<bool>(["--debug"], "Debug on startup") { IsHidden = true };
        rootCmd.AddGlobalOption(debugOpt);
#endif

        var reportCmd = new Command("report", "Generate a report ");

        var pathOpt =
            new Option<DirectoryInfo>(
                ["-p", "--path"],
                "Root path under which the cache and results are stored")
            {
                IsRequired = true
            };

        reportCmd.AddOption(pathOpt);

        var outputOpt = new Option<FileInfo>(["-o", "--output"], "Output filename/path") { IsRequired = true };
        reportCmd.AddOption(outputOpt);

        var lastNOpt = new Option<int>(["-n"], () => 1, "Number of most recent executions to include in the report.");
        reportCmd.AddOption(lastNOpt);

        var formatOpt =
            new Option<ReportCommand.Format>(
                "--format",
                () => ReportCommand.Format.html,
                "Specify the format for the generated report.");

        reportCmd.AddOption(formatOpt);

        reportCmd.SetHandler(
            (path, output, lastN, format) => new ReportCommand(logger).InvokeAsync(path, output, lastN, format),
            pathOpt,
            outputOpt,
            lastNOpt,
            formatOpt);

        rootCmd.Add(reportCmd);

        // TASK: Support more granular filters such as the specific scenario / iteration / execution whose results must
        // be cleaned up.
        var cleanResults = new Command("cleanResults", "Delete results");
        cleanResults.AddOption(pathOpt);

        var lastNOpt2 = new Option<int>(["-n"], () => 0, "Number of most recent executions to preserve.");
        cleanResults.AddOption(lastNOpt2);

        cleanResults.SetHandler(
            (path, lastN) => new CleanResultsCommand(logger).InvokeAsync(path, lastN),
            pathOpt,
            lastNOpt2);

        rootCmd.Add(cleanResults);

        var cleanCache = new Command("cleanCache", "Delete expired cache entries");
        cleanCache.AddOption(pathOpt);

        cleanCache.SetHandler(
            path => new CleanCacheCommand(logger).InvokeAsync(path),
            pathOpt);

        rootCmd.Add(cleanCache);

        // TASK: Support some mechanism to fail a build (i.e. return a failure exit code) based on one or more user
        // specified criteria (e.g., if x% of metrics were deemed 'poor'). Ideally this mechanism would be flexible /
        // extensible enough to allow users to configure multiple different kinds of failure criteria.

#if DEBUG
        ParseResult parseResult = rootCmd.Parse(args);
        if (parseResult.HasOption(debugOpt))
        {
            Debugger.Launch();
        }
#endif

        return await rootCmd.InvokeAsync(args).ConfigureAwait(false);
    }
}
