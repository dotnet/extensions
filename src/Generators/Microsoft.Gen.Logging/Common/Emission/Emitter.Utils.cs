// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Gen.Logging.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging.Emission;

// Stryker disable all

internal sealed partial class Emitter : EmitterBase
{
    private static readonly char[] _specialChars = { '\n', '\r', '"', '\\' };

    internal static string EscapeMessageString(string s)
    {
        int index = s.IndexOfAny(_specialChars);
        if (index < 0)
        {
            return s;
        }

        var sb = new StringBuilder(s.Length);
        _ = sb.Append(s, 0, index);

        while (index < s.Length)
        {
            _ = s[index] switch
            {
                '\n' => sb.Append("\\n"),
                '\r' => sb.Append("\\r"),
                '"' => sb.Append("\\\""),
                '\\' => sb.Append("\\\\"),
                var other => sb.Append(other),
            };

            index++;
        }

        return sb.ToString();
    }

    private static readonly char[] _specialCharsForXmlDocumentation = { '\n', '\r', '<', '>' };

    internal static string EscapeMessageStringForXmlDocumentation(string s)
    {
        int index = s.IndexOfAny(_specialCharsForXmlDocumentation);
        if (index < 0)
        {
            return s;
        }

        var sb = new StringBuilder(s.Length);
        _ = sb.Append(s, 0, index);

        while (index < s.Length)
        {
            _ = s[index] switch
            {
                '\n' => sb.Append("\\n"),
                '\r' => sb.Append("\\r"),
                '<' => sb.Append("&lt;"),
                '>' => sb.Append("&gt;"),
                var other => sb.Append(other),
            };

            index++;
        }

        return sb.ToString();
    }

    internal static IReadOnlyCollection<string> GetLogPropertiesAttributes(LoggingMethod lm)
    {
        var result = new HashSet<string?>();
        var parametersWithLogProps = lm.AllParameters.Where(x => x.HasProperties && !x.HasPropsProvider);
        foreach (var parameter in parametersWithLogProps)
        {
            parameter.TraverseParameterPropertiesTransitively((_, property) => result.Add(property.ClassificationAttributeType));
        }

        // Remove null values (no data classification attribute)
        return result
            .Where(x => x != null)
            .Select(x => x!)
            .ToArray();
    }

    internal static string GetLoggerMethodLogLevel(LoggingMethod lm)
    {
        string level = string.Empty;

        if (lm.Level == null)
        {
            foreach (var p in lm.AllParameters)
            {
                if (p.IsLogLevel)
                {
                    level = p.Name;
                    break;
                }
            }
        }
        else
        {
            level = lm.Level switch
            {
#pragma warning disable S109 // Magic numbers should not be used
                0 => "global::Microsoft.Extensions.Logging.LogLevel.Trace",
                1 => "global::Microsoft.Extensions.Logging.LogLevel.Debug",
                2 => "global::Microsoft.Extensions.Logging.LogLevel.Information",
                3 => "global::Microsoft.Extensions.Logging.LogLevel.Warning",
                4 => "global::Microsoft.Extensions.Logging.LogLevel.Error",
                5 => "global::Microsoft.Extensions.Logging.LogLevel.Critical",
                6 => "global::Microsoft.Extensions.Logging.LogLevel.None",
                _ => $"(global::Microsoft.Extensions.Logging.LogLevel){lm.Level}",
#pragma warning restore S109 // Magic numbers should not be used
            };
        }

        return level;
    }

    internal static string? GetLoggerMethodLogLevelForXmlDocumentation(LoggingMethod lm)
    {
        string level = string.Empty;

        if (lm.Level == null)
        {
            return null;
        }

        return lm.Level switch
        {
#pragma warning disable S109 // Magic numbers should not be used
            0 => "Trace",
            1 => "Debug",
            2 => "Information",
            3 => "Warning",
            4 => "Error",
            5 => "Critical",
            6 => "None",
            _ => $"{lm.Level}",
#pragma warning restore S109 // Magic numbers should not be used
        };
    }

    internal static string EncodeTypeName(string typeName) => typeName.Replace("_", "__").Replace('.', '_');
}
