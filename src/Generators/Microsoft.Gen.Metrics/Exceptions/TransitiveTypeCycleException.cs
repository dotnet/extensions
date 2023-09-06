// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.Metrics.Exceptions;

[SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Internal exception")]
internal sealed class TransitiveTypeCycleException : Exception
{
    public TransitiveTypeCycleException(ISymbol parent, INamedTypeSymbol namedType)
    {
        Parent = parent;
        NamedType = namedType;
    }

    public ISymbol Parent { get; }

    public INamedTypeSymbol NamedType { get; }
}
