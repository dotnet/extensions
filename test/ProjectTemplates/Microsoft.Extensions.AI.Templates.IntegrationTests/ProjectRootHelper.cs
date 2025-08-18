// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.AI.Templates.Tests;

/// <summary>
/// Contains a helper for determining the disk location of the containing project folder.
/// </summary>
/// <remarks>
/// It's important that this file resides in the root of the containing project, or the returned
/// project root path will be incorrect.
/// </remarks>
internal static class ProjectRootHelper
{
    public static string GetThisProjectRoot()
        => GetThisProjectRootCore();

    // This helper method is defined separately from its public variant because it extracts the
    // caller file path via the [CallerFilePath] attribute.
    // Therefore, the caller must be in a known location, i.e., this source file, to produce
    // a reliable result.
    private static string GetThisProjectRootCore([CallerFilePath] string callerFilePath = "")
    {
        if (Path.GetDirectoryName(callerFilePath) is not { Length: > 0 } testProjectRoot)
        {
            throw new InvalidOperationException("Could not determine the root of the test project.");
        }

        return testProjectRoot;
    }
}
