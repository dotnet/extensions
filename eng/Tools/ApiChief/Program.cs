// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using ApiChief.Commands;

namespace ApiChief;

internal static class Program
{
    public static Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Helps with .NET API management activities")
        {
            new Argument<FileInfo>("assembly-path", "Path to the assembly to work with.").ExistingOnly(),

            new Command("emit", "Emits a file resulting from processing the assembly")
            {
                EmitBaseline.Create(),
                EmitDelta.Create(),
                EmitSummary.Create(),
                EmitReview.Create(),
            },

            new Command("check", "Performs checks on the assembly")
            {
                CheckBreakingChanges.Create(),
            }
        };

        return rootCommand.InvokeAsync(args);
    }
}
