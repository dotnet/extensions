// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiagConfig.ConfigStore;

namespace DiagConfig;

internal static class MergeEditorConfigCmd
{
    private sealed class MergeEditorConfigArgs
    {
        public string ConfigDirectory { get; set; } = string.Empty;
        public string EditorConfigFile { get; set; } = string.Empty;
        public string EditorConfigAttribute { get; set; } = string.Empty;
    }

    private static readonly Regex _diagRegex = new(@"^dotnet_diagnostic\.(.*)\.severity *= *(.*)", RegexOptions.Compiled);

    public static Command Create()
    {
        var cmd = new Command("merge", "Merges diagnostic state from a .editorconfig file")
        {
            new Argument<string>("editor-config-file", "Path to the .editorconfig file to read"),
            new Argument<string>("editor-config-attribute", "Attribute for values in the .editorconfig file"),
        };

        cmd.Handler = CommandHandler.Create<MergeEditorConfigArgs>(ExecuteAsync);
        return cmd;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We're doing the right thing")]
    private static Task<int> ExecuteAsync(MergeEditorConfigArgs args)
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
            lines = File.ReadAllLines(args.EditorConfigFile);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to load .editorconfig file: {ex.Message}");
            return Task.FromResult(1);
        }

        var count = 0;
        foreach (var line in lines)
        {
            var match = _diagRegex.Match(line);
            if (match.Success)
            {
                var id = match.Groups[1].Value;
                var severity = match.Groups[2].Value;

                var sev = severity.ToUpperInvariant() switch
                {
                    "ERROR" => Severity.Error,
                    "WARNING" => Severity.Warning,
                    "INFO" => Severity.Suggestion,
                    "SUGGESTION" => Severity.Suggestion,
                    "HIDDEN" => Severity.Silent,
                    "SILENT" => Severity.Silent,
                    "NONE" => Severity.None,
                    "DEFAULT" => Severity.Default,
                    _ => Severity.Error,
                };

                cfg.SetSettingForAttribute(id, args.EditorConfigAttribute, new DiagnosticSetting { Severity = sev });
                count++;
            }
        }

        Console.WriteLine($"Found {count} configured diagnostics");

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
