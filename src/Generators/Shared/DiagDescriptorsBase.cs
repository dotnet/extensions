// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

#pragma warning disable CA1716
namespace Microsoft.Gen.Shared;
#pragma warning restore CA1716

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal class DiagDescriptorsBase
{
    protected static DiagnosticDescriptor Make(
            string id,
            string title,
            string messageFormat,
            string category,
            DiagnosticSeverity defaultSeverity = DiagnosticSeverity.Error,
            bool isEnabledByDefault = true)
    {
#pragma warning disable CA1305 // Specify IFormatProvider
#pragma warning disable CA1863 // Use 'CompositeFormat'
#pragma warning disable CS0436 // Type conflicts with imported type
        return new(
            id,
            title,
            messageFormat,
            category,
            defaultSeverity,
            isEnabledByDefault,
            null,
            string.Format(DiagnosticIds.UrlFormat, id),
            Array.Empty<string>());
#pragma warning restore CS0436 // Type conflicts with imported type
#pragma warning restore CA1863 // Use 'CompositeFormat'
#pragma warning restore CA1305 // Specify IFormatProvider
    }
}
