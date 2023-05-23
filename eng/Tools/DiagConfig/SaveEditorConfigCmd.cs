// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DiagConfig.ConfigStore;

namespace DiagConfig;

internal static class SaveEditorConfigCmd
{
#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable S1144 // Unused private types or members should be removed
    private sealed class SaveEditorConfigArgs
    {
        public string ConfigDirectory { get; set; } = string.Empty;
        public string EditorConfigFile { get; set; } = string.Empty;
        public string EditorConfigAttributes { get; set; } = string.Empty;
        public string Include { get; set; } = string.Empty;
        public string Exclude { get; set; } = string.Empty;
        public bool IsGlobal { get; set; }
        public int MaxTier { get; set; } = int.MaxValue;
    }
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore S3459 // Unassigned members should be removed

    public static Command Create()
    {
        var cmd = new Command("save", "Saves an editor config file")
        {
            new Argument<string>("editor-config-file", "Path to the .editorconfig file to create"),
            new Argument<string>("editor-config-attributes", "Set of comma-separated attributes to evaluate when creating the .editorconfig file"),
            new Option<string>("--include", "Set of comma-separated analyzers to explicitly include in the generated .editorconfig file"),
            new Option<string>("--exclude", "Set of comma-separated analyzers to explicitly exclude in the generated .editorconfig file"),
            new Option<int>("--max-tier", "Maximum tier of diagnostics to enable (0 disables all diagnostics)."),
            new Option<bool>("--is-global", "Adds is_global=true to the generated .editorconfig file"),
        };

        cmd.Handler = CommandHandler.Create<SaveEditorConfigArgs>(ExecuteAsync);
        return cmd;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We're doing the right thing")]
    private static Task<int> ExecuteAsync(SaveEditorConfigArgs args)
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

        var attrs = args.EditorConfigAttributes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var included = args.Include.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var excluded = args.Exclude.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        try
        {
            cfg.ExportEditorConfig(attrs, included, excluded, args.EditorConfigFile, args.MaxTier, args.IsGlobal);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to save file {args.EditorConfigFile}: {ex.Message}");
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }
}
