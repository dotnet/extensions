// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.Logging.Parsing;

[SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Internal exception")]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Internal exception")]
internal sealed class MultipleDataClassesAppliedException : Exception
{
    public MultipleDataClassesAppliedException(IPropertySymbol property)
    {
        Property = property;
    }

    public IPropertySymbol Property { get; }
}
