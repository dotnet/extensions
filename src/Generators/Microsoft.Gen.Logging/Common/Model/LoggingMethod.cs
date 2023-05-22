// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Gen.Logging.Model;

/// <summary>
/// A logger method in a logger type.
/// </summary>
internal sealed class LoggingMethod
{
    public readonly List<LoggingMethodParameter> AllParameters = new();
    public readonly List<LoggingMethodParameter> TemplateParameters = new();
    public readonly Dictionary<string, string> TemplateMap = new(StringComparer.OrdinalIgnoreCase);
    public readonly List<string> TemplateList = new();
    public string Name = string.Empty;
    public string Message = string.Empty;
    public int? Level;
    public int? EventId;
    public string? EventName;
    public bool SkipEnabledCheck;
    public bool IsExtensionMethod;
    public bool IsStatic;
    public string Modifiers = string.Empty;
    public string LoggerField = "_logger";
    public string RedactorProviderField = "_redactorProvider";
    public bool LoggerFieldNullable;
    public bool RedactorProviderFieldNullable;
    public bool HasXmlDocumentation;

    public string GetParameterNameInTemplate(LoggingMethodParameter parameter)
        => TemplateMap.TryGetValue(parameter.Name, out var value)
            ? value
            : parameter.Name;
}
