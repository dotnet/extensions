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

internal static class CheckBreakingChanges
{
    [SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "Written through reflection.")]
    private sealed class CheckBreakingChangesArgs
    {
        public FileInfo? AssemblyPath { get; }

        public string BaselinePath { get; } = string.Empty;
    }

    public static Command Create()
    {
        var command = new Command("breaking", "Performs a breaking change check")
        {
            new Argument<string>("baseline-path", "Path to the baseline report to use for reference"),
        };

        command.Handler = CommandHandler.Create<CheckBreakingChangesArgs>(ExecuteAsync);

        return command;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching for proper error reporting.")]
    private static Task<int> ExecuteAsync(CheckBreakingChangesArgs args)
    {
        ApiModel current;
        ApiModel baseline;

        try
        {
            current = ApiModel.LoadFromAssembly(args.AssemblyPath!.FullName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to create the current API baseline report from '{args.AssemblyPath!.FullName}': {ex.Message}");

            return Task.FromResult(-1);
        }

        try
        {
            baseline = ApiModel.LoadFromFile(args.BaselinePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to load previous API baseline report '{args.BaselinePath}': {ex.Message}");

            return Task.FromResult(-1);
        }

        baseline.EvaluateDelta(current);

        if (current.Removals != null)
        {
            Console.Error.WriteLine($"Detected removed APIs in the current baseline report: {string.Join(';', current.Removals)}");

            return Task.FromResult(-1);
        }

        return Task.FromResult(0);
    }
}
