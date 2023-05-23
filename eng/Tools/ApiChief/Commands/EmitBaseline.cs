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

internal static class EmitBaseline
{
    [SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "Written through reflection.")]
    [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Written through reflection.")]
    private sealed class EmitBaselineArgs
    {
        public FileInfo? AssemblyPath { get; set; }
        public string? Output { get; set; }
    }

    public static Command Create()
    {
        var cmd = new Command("baseline", "Creates an API baseline")
        {
            new Option<string>(
                new[] { "-o", "--output" },
                "Path of the baseline file to produce"),
        };

        cmd.Handler = CommandHandler.Create<EmitBaselineArgs>(ExecuteAsync);
        return cmd;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching for proper error reporting.")]
    private static async Task<int> ExecuteAsync(EmitBaselineArgs args)
    {
        ApiModel model;
        try
        {
            model = ApiModel.LoadFromAssembly(args.AssemblyPath!.FullName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to decompile assembly '{args.AssemblyPath!.FullName}': {ex.Message}");
            return -1;
        }

        var result = model.ToString();

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
                Console.Error.WriteLine($"Unable to write output baseline report '{args.Output}': {ex.Message}");
                return -1;
            }
        }

        return 0;
    }
}
