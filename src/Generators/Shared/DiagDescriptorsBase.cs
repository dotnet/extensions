// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

#pragma warning disable CA1716
namespace Microsoft.Gen.Shared;
#pragma warning restore CA1716

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal class DiagDescriptorsBase
{
#pragma warning disable S1075 // URIs should not be hardcoded
    public const string HelpLinkBase = "https://eng.ms/docs/experiences-devices/r9-sdk/docs/code-generation/generators/";
#pragma warning restore S1075 // URIs should not be hardcoded

    protected static DiagnosticDescriptor Make(
            string id,
            string title,
            string messageFormat,
            string category,
            DiagnosticSeverity defaultSeverity = DiagnosticSeverity.Error,
            bool isEnabledByDefault = true)
    {
        return new(
            id,
            title,
            messageFormat,
            category,
            defaultSeverity,
            isEnabledByDefault,
            null,
#pragma warning disable CA1308 // Normalize strings to uppercase
            HelpLinkBase + id.ToLowerInvariant(),
#pragma warning restore CA1308 // Normalize strings to uppercase
            Array.Empty<string>());
    }
}
