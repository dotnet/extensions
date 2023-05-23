// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Threading.Tasks;

namespace DiagConfig;

internal static class Program
{
    public static Task<int> Main(string[] args)
    {
        var rootCmd = new RootCommand("Manipulates diagnostic config files and .editorconfig files")
        {
            new Argument<string>("config-directory", "Path to the directory holding the diagnostic config YAML files."),

            new Command("analyzer", "Interact with Roslyn analyzers")
            {
                MergeAnalyzersCmd.Create(),
            },

            new Command("editorconfig", "Interact with .editorconfig files")
            {
                MergeEditorConfigCmd.Create(),
                SaveEditorConfigCmd.Create()
            },

            new Command("csv", "Interact with comma-separated value files")
            {
                LoadCSVCmd.Create(),
                SaveCSVCmd.Create()
            },

            ShowAttributesCmd.Create(),
        };

        return rootCmd.InvokeAsync(args);
    }
}
