// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Extensions.LocalAnalyzers.Resource.Test;

internal static class AnalyzerOptionsFactory
{
    public static AnalyzerOptions WithFiles(params string[] fileNames)
    {
        var files = fileNames.Select(name => (AdditionalText)new FileVisibleToAnalyzer(name)).ToArray();
        var immutableFiles = ImmutableArray.Create(files, 0, files.Length);

        return new AnalyzerOptions(immutableFiles);
    }
}
