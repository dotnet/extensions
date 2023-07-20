// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.ExtraAnalyzers.Utilities;

internal static class CompilationExtensions
{
    public static bool IsNet6OrGreater(this Compilation compilation)
    {
        var type = compilation.GetTypeByMetadataName("System.Environment");
        return type != null && type.GetMembers("ProcessPath").Length > 0;
    }
}
