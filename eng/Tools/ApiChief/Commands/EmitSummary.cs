// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using ApiChief.Format;
using ApiChief.Processing;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;

namespace ApiChief.Commands;

internal static class EmitSummary
{
    [SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "Written through reflection.")]
    [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Written through reflection.")]
    private sealed class EmitSummaryArgs
    {
        public FileInfo? AssemblyPath { get; set; }
        public string? Output { get; set; }
        public bool OmitXmlComments { get; set; }
    }

    public static Command Create()
    {
        var command = new Command("summary", "Creates an API summary")
        {
            new Option<string>(
                new[] { "-o", "--output" },
                "Path of the summary file to produce"),

            new Option<bool>(
                new[] { "-x", "--omit-xml-comments" },
                "Omit the XML documentation comments"),
        };

        command.Handler = CommandHandler.Create<EmitSummaryArgs>(ExecuteAsync);
        return command;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching for proper error reporting.")]
    private static async Task<int> ExecuteAsync(EmitSummaryArgs args)
    {
        var formatting = args.OmitXmlComments ? Formatter.BaselineFormatting : Formatter.FormattingWithXmlComments;
        CSharpDecompiler decompiler;

        try
        {
            var path = args.AssemblyPath!.FullName;

            decompiler = args.OmitXmlComments
                ? DecompilerFactory.Create(path)
                : DecompilerFactory.CreateWithXmlComments(path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to decompile assembly '{args.AssemblyPath!.FullName}': {ex.Message}");
            return -1;
        }

        var syntaxTree = decompiler.DecompileWholeModuleAsSingleFile(true);

        // remove private stuff from the tree
        syntaxTree.AcceptVisitor(new PublicFilterVisitor());

        using var writer = new StringWriter();
        var visitor = new CSharpOutputVisitor(writer, formatting);
        syntaxTree.AcceptVisitor(visitor);
        var result = writer.ToString();

        if (args.Output == null)
        {
            Console.Write(result);
        }
        else
        {
            try
            {
                await File.WriteAllTextAsync(args.Output, result);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to write output summary file '{args.Output}': {ex.Message}");
                return -1;
            }
        }

        return 0;
    }
}
