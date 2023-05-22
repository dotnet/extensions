// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.Logging.Parsing;

internal static class SymbolLoader
{
    internal const string LogMethodAttribute = "Microsoft.Extensions.Telemetry.Logging.LogMethodAttribute";
    internal const string LogPropertiesAttribute = "Microsoft.Extensions.Telemetry.Logging.LogPropertiesAttribute";
    internal const string LogPropertyIgnoreAttribute = "Microsoft.Extensions.Telemetry.Logging.LogPropertyIgnoreAttribute";
    internal const string ILogPropertyCollectorType = "Microsoft.Extensions.Telemetry.Logging.ILogPropertyCollector";
    internal const string ILoggerType = "Microsoft.Extensions.Logging.ILogger";
    internal const string IRedactorProviderType = "Microsoft.Extensions.Compliance.Redaction.IRedactorProvider";
    internal const string LogLevelType = "Microsoft.Extensions.Logging.LogLevel";
    internal const string ExceptionType = "System.Exception";
    internal const string DataClassificationAttribute = "Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute";
    internal const string LogMethodHelper = "Microsoft.Extensions.Telemetry.Logging.LogMethodHelper";
    internal const string IEnrichmentPropertyBag = "Microsoft.Extensions.Telemetry.Enrichment.IEnrichmentPropertyBag";
    internal const string IFormatProviderType = "System.IFormatProvider";

    private static readonly string[] _ignored = new[]
    {
        "System.DateTimeOffset",
        "System.Guid",
        "System.TimeSpan",
        "System.TimeOnly",
        "System.DateOnly",
        "System.Version",
        "System.Uri",
        "System.Net.IPAddress",
        "System.Net.EndPoint",
        "System.Net.IPEndPoint",
        "System.Net.DnsEndPoint",
        "System.Numerics.BigInteger",
        "System.Numerics.Complex",
        "System.Numerics.Matrix3x2",
        "System.Numerics.Matrix4x4",
        "System.Numerics.Plane",
        "System.Numerics.Quaternion",
        "System.Numerics.Vector2",
        "System.Numerics.Vector3",
        "System.Numerics.Vector4",
    };

    internal static SymbolHolder? LoadSymbols(
        Compilation compilation,
        Action<DiagnosticDescriptor, Location?, object?[]?> diagCallback)
    {
        var loggerSymbol = compilation.GetTypeByMetadataName(ILoggerType);
        var logLevelSymbol = compilation.GetTypeByMetadataName(LogLevelType);
        var logMethodAttributeSymbol = compilation.GetTypeByMetadataName(LogMethodAttribute);
        var logPropertiesAttributeSymbol = compilation.GetTypeByMetadataName(LogPropertiesAttribute);
        var logPropertyCollectorSymbol = compilation.GetTypeByMetadataName(ILogPropertyCollectorType);
        var logPropertyIgnoreAttributeSymbol = compilation.GetTypeByMetadataName(LogPropertyIgnoreAttribute);
        var dataClassificationAttribute = compilation.GetTypeByMetadataName(DataClassificationAttribute);

#pragma warning disable S1067 // Expressions should not be too complex
        if (loggerSymbol == null
            || logLevelSymbol == null
            || logMethodAttributeSymbol == null
            || logPropertiesAttributeSymbol == null
            || logPropertyCollectorSymbol == null
            || logPropertyIgnoreAttributeSymbol == null)
        {
            // nothing to do if these types aren't available
            return null;
        }
#pragma warning restore S1067 // Expressions should not be too complex

        var exceptionSymbol = compilation.GetTypeByMetadataName(ExceptionType);
        if (exceptionSymbol == null)
        {
            diagCallback(DiagDescriptors.MissingRequiredType, null, new object[] { ExceptionType });
            return null;
        }

        var logMethodHelperSymbol = compilation.GetTypeByMetadataName(LogMethodHelper);
        var enrichmentPropBagSymbol = compilation.GetTypeByMetadataName(IEnrichmentPropertyBag);

        if (logMethodHelperSymbol != null)
        {
            bool found = false;
            foreach (var iface in logMethodHelperSymbol.Interfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface, enrichmentPropBagSymbol))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                logMethodHelperSymbol = null;
            }
        }

        var redactorProviderSymbol = compilation.GetTypeByMetadataName(IRedactorProviderType);
        var enumerableSymbol = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
        var formatProviderSymbol = compilation.GetTypeByMetadataName(IFormatProviderType)!;

        var ignorePropsSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var ign in _ignored)
        {
            var s = compilation.GetTypeByMetadataName(ign);
            if (s != null)
            {
                _ = ignorePropsSymbols.Add(s);
            }
        }

        return new(
            compilation,
            logMethodAttributeSymbol,
            logPropertiesAttributeSymbol,
            logPropertyIgnoreAttributeSymbol,
            logPropertyCollectorSymbol,
            loggerSymbol,
            redactorProviderSymbol,
            logLevelSymbol,
            exceptionSymbol,
            ignorePropsSymbols,
            enumerableSymbol,
            formatProviderSymbol,
            dataClassificationAttribute);
    }
}
