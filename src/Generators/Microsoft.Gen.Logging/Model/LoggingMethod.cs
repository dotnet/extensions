// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Gen.Logging.Model;

/// <summary>
/// A logger method in a logger type.
/// </summary>
[DebuggerDisplay("{Name}")]
internal sealed class LoggingMethod
{
    public readonly List<LoggingMethodParameter> Parameters = [];
    public readonly List<string> Templates = [];
    public string Name = string.Empty;
    public string Message = string.Empty;
    public int? Level;
    public int? EventId;
    public string? EventName;
    public bool SkipEnabledCheck;
    public bool IsExtensionMethod;
    public bool IsStatic;
    public string Modifiers = string.Empty;
    public string LoggerMember = "_logger";
    public bool LoggerMemberNullable;
    public bool HasXmlDocumentation;

    public LoggingMethodParameter? GetParameterForTemplate(string templateName)
    {
        foreach (var p in Parameters)
        {
            if (templateName.Equals(p.ParameterName, StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }
        }

        return null;
    }

    public List<string> GetTemplatesForParameter(LoggingMethodParameter lp)
        => GetTemplatesForParameter(lp.ParameterName);

    public List<string> GetTemplatesForParameter(string parameterName)
    {
        HashSet<string> templates = [];
        foreach (var t in Templates)
        {
            if (parameterName.Equals(t, StringComparison.OrdinalIgnoreCase))
            {
                _ = templates.Add(t);
            }
        }

        return templates.ToList();
    }
}
