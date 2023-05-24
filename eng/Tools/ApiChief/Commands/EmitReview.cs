﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApiChief.Format;
using ApiChief.Processing;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.TypeSystem;

namespace ApiChief.Commands;

internal static class EmitReview
{
    [SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "Written through reflection.")]
    [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Written through reflection.")]
    private sealed class EmitReviewArgs
    {
        public FileInfo? AssemblyPath { get; set; }
        public string? Output { get; set; }
        public bool GroupByNamespace { get; set; }
    }

    public static Command Create()
    {
        var command = new Command("review", "Produces a folder with files to submit for API reviews")
        {
            new Option<string>(
                new[] { "-o", "--output" },
                "Path of the directory to receive the API review files"),

            new Option<bool>(
                new[] { "-n", "--group-by-namespace" },
                "Group output by namespace rather than assembly"),
        };

        command.Handler = CommandHandler.Create<EmitReviewArgs>(ExecuteAsync);
        return command;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching for proper error reporting.")]
    private static async Task<int> ExecuteAsync(EmitReviewArgs args)
    {
        // once with XML comments, once without
#pragma warning disable S109 // Magic numbers should not be used
        for (int i = 0; i < 2; i++)
        {
            var formatting = Formatter.FormattingWithXmlComments;
            var outputAsmDep = true;
            var sub = "WithDocs";
            var omitXmlComments = false;

            if (i == 1)
            {
                formatting = Formatter.BaselineFormatting;
                outputAsmDep = false;
                sub = "NoDocs";
                omitXmlComments = true;
            }

            if (args.GroupByNamespace)
            {
                outputAsmDep = false;
            }

            CSharpDecompiler decompiler;
            try
            {
                var path = args.AssemblyPath!.FullName;

                var decompilerSettings = new DecompilerSettings(LanguageVersion.Latest)
                {
                    DecompileMemberBodies = false,
                    ExpandUsingDeclarations = true,
                    UsingDeclarations = true,
                    ShowXmlDocumentation = false,
                    CSharpFormattingOptions = Formatter.BaselineFormatting,
                    AsyncAwait = false,
                    AlwaysShowEnumMemberValues = true,
                };

                if (!omitXmlComments)
                {
                    decompilerSettings.CSharpFormattingOptions = Formatter.FormattingWithXmlComments;
                    decompilerSettings.ShowXmlDocumentation = true;
                }

                decompiler = new(path, decompilerSettings);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to decompile assembly '{args.AssemblyPath!.FullName}': {ex.Message}");
                return -1;
            }

            var output = args.Output;
            if (args.GroupByNamespace)
            {
                if (output == null)
                {
                    output = "API";
                }
            }
            else
            {
                if (output == null)
                {
                    output = "API." + decompiler.TypeSystem.MainModule.AssemblyName;
                }
                else
                {
                    output = Path.Combine(output, decompiler.TypeSystem.MainModule.AssemblyName);
                }
            }

            if (outputAsmDep)
            {
                var sb = new StringBuilder();
                foreach (var m in decompiler.TypeSystem.ReferencedModules.OrderBy(m => m.AssemblyName))
                {
                    sb.AppendLine(m.AssemblyName);
                }

                try
                {
                    Directory.CreateDirectory(output);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unable to create output directory '{args.Output}': {ex.Message}");
                    return -1;
                }

                var asmDepFile = Path.Combine(output, "AssemblyDependencies.txt");
                try
                {
                    await File.WriteAllTextAsync(asmDepFile, sb.ToString());
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unable to write API dependency file '{asmDepFile}': {ex.Message}");
                    return -1;
                }
            }

            output += "/" + sub;

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var type in decompiler.TypeSystem.GetTopLevelTypeDefinitions().OrderBy(t => t.MetadataName))
            {
                var acc = type.EffectiveAccessibility();
                if (acc is not Accessibility.Public and not Accessibility.Protected and not Accessibility.ProtectedOrInternal)
                {
                    continue;
                }

                if (type.ParentModule == null || !type.ParentModule.IsMainModule)
                {
                    continue;
                }

                var syntaxTree = decompiler.DecompileType(type.FullTypeName);

                // remove private stuff from the tree
                syntaxTree.AcceptVisitor(new PublicFilterVisitor());

                using var writer = new StringWriter();

                writer.WriteLine($"// Assembly '{type.ParentModule.AssemblyName}'");
                writer.WriteLine();

                var visitor = new CSharpOutputVisitor(writer, formatting);
                syntaxTree.AcceptVisitor(visitor);
                var result = writer.ToString();

                var file = type.FullName.Replace("<", "_").Replace(">", "_");
                var index = file.LastIndexOf(".", StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    file = file.Substring(0, index) + "/" + file.Substring(index + 1);
                }

                var dir = Path.GetDirectoryName(file) ?? string.Empty;
                var fullDir = Path.Combine(output, dir);
                var fullFile = Path.Combine(fullDir, Path.GetFileName(file));

                var tmp = fullFile;

                if (names.Contains(tmp))
                {
                    if (type.TypeParameters.Count > 0)
                    {
                        var sb = new StringBuilder();
                        foreach (var t in type.TypeParameters)
                        {
                            sb.Append('.');
                            sb.Append(t.Name);
                        }

                        tmp += sb;
                    }

                    var count = 2;
                    while (names.Contains(tmp))
                    {
                        tmp = fullFile + "_" + count++;
                    }
                }

                names.Add(tmp);
                fullFile = tmp + ".cs";

                try
                {
                    Directory.CreateDirectory(fullDir);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unable to create output directory '{fullDir}': {ex.Message}");
                    return -1;
                }

                try
                {
                    await File.WriteAllTextAsync(fullFile, result);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unable to write output API review file '{fullFile}': {ex.Message}");
                    return -1;
                }
            }
        }
#pragma warning restore S109 // Magic numbers should not be used

        return 0;
    }
}
