// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Gen.Logging.Model;

/// <summary>
/// A single parameter to a logger method.
/// </summary>
[DebuggerDisplay("{Name}")]
internal sealed class LoggingMethodParameter
{
    public string Name = string.Empty;
    public string Type = string.Empty;
    public string? Qualifier;
    public bool NeedsAtSign;
    public bool IsLogger;
    public bool IsRedactorProvider;
    public bool IsException;
    public bool IsLogLevel;
    public bool IsEnumerable;
    public bool IsNullable;
    public bool IsReference;
    public bool ImplementsIConvertible;
    public bool ImplementsIFormattable;
    public bool SkipNullProperties;
    public bool OmitParameterName;
    public bool UsedAsTemplate;
    public string? ClassificationAttributeType;
    public List<LoggingProperty> PropertiesToLog = new();
    public LoggingPropertyProvider? LogPropertiesProvider;

    public string NameWithAt => NeedsAtSign ? "@" + Name : Name;

    public string PotentiallyNullableType
        => (IsReference && !IsNullable)
            ? Type + "?"
            : Type;

    // A parameter flagged as 'normal' is not going to be taken care of specially as an argument to ILogger.Log
    // but instead is supposed to be taken as a normal parameter.
    public bool IsNormalParameter => !IsLogger && !IsRedactorProvider && !IsException && !IsLogLevel;

    public bool HasProperties => PropertiesToLog.Count > 0;
    public bool HasPropsProvider => LogPropertiesProvider is not null;
    public bool PotentiallyNull => (IsReference && !IsLogger && !IsRedactorProvider) || IsNullable;
}
