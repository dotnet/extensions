// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Gen.Logging.Model;

[DebuggerDisplay("{Name}")]
[ExcludeFromCodeCoverage]
internal sealed record LoggingProperty(
    string Name,
    string Type,
    string? ClassificationAttributeType,
    bool NeedsAtSign,
    bool IsNullable,
    bool IsReference,
    bool IsEnumerable,
    bool ImplementsIConvertible,
    bool ImplementsIFormatable,
    IReadOnlyCollection<LoggingProperty> TransitiveMembers)
{
    public string NameWithAt => NeedsAtSign ? "@" + Name : Name;
    public bool PotentiallyNull => IsReference || IsNullable;
}
