// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DiagConfig.ConfigStore;

namespace DiagConfig;

internal static class ShowAttributesCmd
{
    private sealed class ShowAttributesArgs
    {
        public string ConfigDirectory { get; set; } = string.Empty;
    }

    public static Command Create()
    {
        return new Command("attributes", "Show the set of available attributes")
        {
            Handler = CommandHandler.Create<ShowAttributesArgs>(ExecuteAsync),
        };
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We're doing the right thing")]
    private static Task<int> ExecuteAsync(ShowAttributesArgs args)
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

        var attributes = cfg.GetAttributes();

        foreach (var a in attributes)
        {
            Console.WriteLine(a);
        }

        return Task.FromResult(0);
    }
}
