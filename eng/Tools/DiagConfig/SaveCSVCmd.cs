// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DiagConfig.ConfigStore;

namespace DiagConfig;

internal static class SaveCSVCmd
{
    private sealed class SaveCSVArgs
    {
        public string ConfigDirectory { get; set; } = string.Empty;
        public string CSVFile { get; set; } = string.Empty;
    }

    public static Command Create()
    {
        var cmd = new Command("save", "Saves analyzer state as a comma-separated value file")
        {
            new Argument<string>("csv-file", "Path to the CSV file to create"),
        };

        cmd.Handler = CommandHandler.Create<SaveCSVArgs>(ExecuteAsync);
        return cmd;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We're doing the right thing")]
    private static Task<int> ExecuteAsync(SaveCSVArgs args)
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

        var sb = new StringBuilder();
        _ = sb.AppendLine("Analyzer,Diagnostic,Tier,Title,Category,Default Severity,Description");
        foreach (var kvp in cfg.Analyzers)
        {
            var analyzer = kvp.Key;
            foreach (var d in kvp.Value.Diagnostics)
            {
                _ = sb.AppendLine($"{analyzer},"
                    + $"{d.Key},"
                    + $"{d.Value.Tier},"
                    + $"{d.Value.Metadata.Title},"
                    + $"{d.Value.Metadata.Category},"
                    + $"{d.Value.Metadata.DefaultSeverity},"
                    + $"{d.Value.Metadata.Description.Replace(",", "-")}");
            }
        }

        try
        {
            File.WriteAllText(args.CSVFile, sb.ToString().ReplaceLineEndings("\n"));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to write to file `{args.CSVFile}`: {ex.Message}");
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }
}
