// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.TestUtilities;

namespace Microsoft.Extensions.DataIngestion.Readers.Tests;

/// <summary>
/// This class exists because currently the local copy of <see cref="ConditionalTheoryAttribute"/> can't ignore tests that throw <see cref="SkipTestException"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class MarkItDownConditionAttribute : Attribute, ITestCondition
{
    internal static readonly Lazy<bool> IsInstalled = new(CanInvokeMarkItDown);

    public bool IsMet => IsInstalled.Value;

    public string SkipReason => "MarkItDown is not installed or not accessible.";

    private static bool CanInvokeMarkItDown()
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "markitdown",
            Arguments = "--help",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        using Process process = new() { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            return false;
        }

        while (process.StandardOutput.Peek() >= 0)
        {
            _ = process.StandardOutput.ReadLine();
        }

        process.WaitForExit();

        return true;
    }
}
