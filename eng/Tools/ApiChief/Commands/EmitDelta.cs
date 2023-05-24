// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using ApiChief.Model;

namespace ApiChief.Commands;

internal static class EmitDelta
{
    [SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "Written through reflection.")]
    [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Written through reflection.")]
    private sealed class EmitDeltaArgs
    {
        public FileInfo? AssemblyPath { get; set; }

        public string BaselinePath { get; set; } = string.Empty;

        public string? Output { get; set; }
    }

    public static Command Create()
    {
        var command = new Command("delta", "Creates an API delta")
        {
            new Argument<string>("baseline-path", "Path to the baseline report to use for reference"),

            new Option<string>(
                new[] { "-o", "--output" },
                "Path of the delta file to produce"),
        };

        command.Handler = CommandHandler.Create<EmitDeltaArgs>(ExecuteAsync);
        return command;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching for proper error reporting.")]
    private static async Task<int> ExecuteAsync(EmitDeltaArgs args)
    {
        ApiModel current;
        ApiModel baseline;

        try
        {
            current = ApiModel.LoadFromAssembly(args.AssemblyPath!.FullName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to decompile assembly '{args.AssemblyPath!.FullName}': {ex.Message}");
            return -1;
        }

        try
        {
            baseline = ApiModel.LoadFromFile(args.BaselinePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to load baseline report '{args.BaselinePath}': {ex.Message}");
            return -1;
        }

        baseline.EvaluateDelta(current);

        var result = current.ToString();

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
                Console.Error.WriteLine($"Unable to write output delta report '{args.Output}': {ex.Message}");
                return -1;
            }
        }

        return 0;
    }
}
