﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Gen.Logging.Model;

[DebuggerDisplay("{PropertyName}")]
internal sealed class LoggingProperty
{
    public string PropertyName = string.Empty;
    public string TagName = string.Empty;
    public string Type = string.Empty;
    public HashSet<string> ClassificationAttributeTypes = [];
    public bool NeedsAtSign;
    public bool IsNullable;
    public bool IsReference;
    public bool IsEnumerable;
    public bool ImplementsIConvertible;
    public bool ImplementsIFormattable;
    public bool ImplementsISpanFormattable;
    public bool HasCustomToString;
    public List<LoggingProperty> Properties = [];
    public bool OmitReferenceName;
    public TagProvider? TagProvider;

    public bool HasDataClassification => ClassificationAttributeTypes.Count > 0;
    public bool HasProperties => Properties.Count > 0;
    public bool HasTagProvider => TagProvider is not null;
    public string PropertyNameWithAt => NeedsAtSign ? "@" + PropertyName : PropertyName;
    public bool PotentiallyNull => IsReference || IsNullable;
    public bool IsStringifiable => HasCustomToString || ImplementsIConvertible || ImplementsIFormattable || IsEnumerable;
}
