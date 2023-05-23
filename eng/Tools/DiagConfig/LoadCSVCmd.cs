// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiagConfig.ConfigStore;

namespace DiagConfig;

internal static class LoadCSVCmd
{
    private const int AnalyzerNamePart = 0;
    private const int DiagnosticPart = 1;
    private const int TierPart = 2;

    private sealed class LoadCSVArgs
    {
        public string ConfigDirectory { get; set; } = string.Empty;
        public string CSVFile { get; set; } = string.Empty;
    }

    public static Command Create()
    {
        var cmd = new Command("load-tiers", "Loads diagnostic tier state from a comma-separated value file")
        {
            new Argument<string>("csv-file", "Path to the CSV file to process"),
        };

        cmd.Handler = CommandHandler.Create<LoadCSVArgs>(ExecuteAsync);
        return cmd;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We're doing the right thing")]
    private static Task<int> ExecuteAsync(LoadCSVArgs args)
    {
        Store cfg;
        try
        {
            cfg = Store.Load(args.ConfigDirectory);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to load diagnostic configuration state: {ex.Message}");
            return Task.FromResult(1);
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(args.CSVFile);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to read CSV file `{args.CSVFile}`: {ex.Message}");
            return Task.FromResult(1);
        }

        int count = 1;
        foreach (var l in lines.Skip(1))
        {
            var parts = l.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 8)
            {
                Console.Error.WriteLine($"Invalid format on line {count}");
                return Task.FromResult(1);
            }

            var analyzer = parts[AnalyzerNamePart];
            var diag = parts[DiagnosticPart];

            if (!int.TryParse(parts[TierPart], NumberStyles.Any, CultureInfo.InvariantCulture, out var tier))
            {
                Console.Error.WriteLine($"Invalid format on line {count}");
                return Task.FromResult(1);
            }

            if (cfg.Analyzers.TryGetValue(analyzer, out var a)
                && a.Diagnostics.TryGetValue(diag, out var d))
            {
                d.Tier = tier;
            }
            else
            {
                Console.Error.WriteLine($"Couldn't find diagnostics {diag} from analyzer {analyzer}, skipping");
            }

            count++;
        }

        try
        {
            cfg.Save(args.ConfigDirectory);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to save diagnostic configuration state: {ex.Message}");
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }
}
