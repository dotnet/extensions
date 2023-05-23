// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DiagConfig.ConfigStore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DiagConfig;

internal static class MergeAnalyzersCmd
{
    private sealed class MergeAnalyzersArgs
    {
        public string ConfigDirectory { get; set; } = string.Empty;
        public string[] Analyzers { get; set; } = Array.Empty<string>();
    }

    public static Command Create()
    {
        var cmd = new Command("merge", "Merges diagnostic descriptors from Roslyn analyzer assemblies")
        {
            new Argument<string[]>("analyzers", "Paths to analyzer assemblies")
            {
                Arity = ArgumentArity.OneOrMore,
            },
        };

        cmd.Handler = CommandHandler.Create<MergeAnalyzersArgs>(ExecuteAsync);
        return cmd;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We're doing the right thing")]
    private static Task<int> ExecuteAsync(MergeAnalyzersArgs args)
    {
        var diag = typeof(DiagnosticAnalyzer);
        var diagAttr = typeof(DiagnosticAnalyzerAttribute);

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

        foreach (var path in args.Analyzers)
        {
            Assembly asm;
            try
            {
#pragma warning disable S3885 // "Assembly.Load" should be used
                asm = Assembly.LoadFrom(path);
#pragma warning restore S3885 // "Assembly.Load" should be used
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to load assembly {path}: {ex.Message}");
                return Task.FromResult(1);
            }

            var name = asm.GetName().Name!;
            var types = asm.GetTypes();
            var analyzers = types.Where(t => t.IsAssignableTo(diag) && t.GetCustomAttribute(diagAttr) != null);

            var origin = new Origin
            {
                AssemblyName = name,
            };

            if (asm.GetCustomAttribute(typeof(AssemblyFileVersionAttribute)) is AssemblyFileVersionAttribute fileVer)
            {
                origin.Version = fileVer.Version;
            }

            int diagCount = 0;
            foreach (var at in analyzers)
            {
                var da = (DiagnosticAnalyzer)at.GetConstructor(Array.Empty<Type>())!.Invoke(null);
                var diags = da.SupportedDiagnostics;
                foreach (var d in diags)
                {
                    var sev = d.DefaultSeverity switch
                    {
                        DiagnosticSeverity.Hidden => Severity.None,
                        DiagnosticSeverity.Info => Severity.Suggestion,
                        DiagnosticSeverity.Warning => Severity.Warning,
                        DiagnosticSeverity.Error => Severity.Error,
                        _ => Severity.Error
                    };

                    var meta = new ConfigStore.Metadata
                    {
                        Category = d.Category,
                        Title = d.Title.ToString(CultureInfo.InvariantCulture),
                        Description = d.Description.ToString(CultureInfo.InvariantCulture),
                        HelpLinkUri = d.HelpLinkUri,
                        CustomTags = (d.CustomTags != null && d.CustomTags.Any()) ? new List<string>(d.CustomTags) : null,
                        DefaultSeverity = sev,
                    };

                    var dg = new ConfigStore.Diagnostic
                    {
                        Metadata = meta,
                        Attributes =
                        {
                            ["general"] = new DiagnosticSetting
                            {
                                Severity = sev,
                            },
                        },
                    };

                    cfg.Merge(origin, d.Id, dg);
                    diagCount++;
                }
            }

            Console.WriteLine($"Assembly {name}: {diagCount} diagnostics found");
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
