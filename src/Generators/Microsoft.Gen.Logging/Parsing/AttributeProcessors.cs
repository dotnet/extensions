// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.Logging.Parsing;

internal static class AttributeProcessors
{
    private const string EventNameProperty = "EventName";
    private const string SkipEnabledCheckProperty = "SkipEnabledCheck";
    private const string SkipNullProperties = "SkipNullProperties";
    private const string OmitParameterName = "OmitParameterName";

    private const int LogLevelError = 4;
    private const int LogLevelCritical = 5;

    public static (int? eventId, int? level, string message, string? eventName, bool skipEnabledCheck)
        ExtractLogMethodAttributeValues(AttributeData attr, SymbolHolder symbols)
    {
        // seven constructor arg shapes:
        //
        //   (int eventId, LogLevel level, string message)
        //   (int eventId, LogLevel level)
        //   (LogLevel level, string message)
        //   (LogLevel level)
        //   (string message)
        //   (int eventId, string message)
        //   ()

        int? eventId = null;
        int? level = null;
        string? eventName = null;
        string message = string.Empty;
        bool skipEnabledCheck = false;
        bool useDefaultForSkipEnabledCheck = true;

        foreach (var a in attr.ConstructorArguments)
        {
            if (SymbolEqualityComparer.Default.Equals(a.Type, symbols.LogLevelSymbol))
            {
                var v = a.Value;
                if (v is int l)
                {
                    level = l;
                }
            }
            else if (a.Type != null && a.Type.SpecialType == SpecialType.System_Int32)
            {
                var v = a.Value;
                if (v is int l)
                {
                    eventId = l;
                }
            }
            else
            {
                message = a.Value as string ?? string.Empty;
            }
        }

        foreach (var a in attr.NamedArguments)
        {
            switch (a.Key)
            {
                case EventNameProperty:
                    eventName = a.Value.Value as string;
                    break;

                case SkipEnabledCheckProperty:
                    skipEnabledCheck = (bool)a.Value.Value!;
                    useDefaultForSkipEnabledCheck = false;
                    break;
            }
        }

        if (level != null)
        {
            if (useDefaultForSkipEnabledCheck && (level == LogLevelError || level == LogLevelCritical))
            {
                // unless explicitly set by the user, by default we disable the Enabled check when the log level is Error or Critical
                skipEnabledCheck = true;
            }
        }

        return (eventId, level, message, eventName, skipEnabledCheck);
    }

    public static (bool skipNullProperties, bool omitParameterName, ITypeSymbol? providerType, string? providerMethodName)
        ExtractLogPropertiesAttributeValues(AttributeData attr)
    {
        bool skipNullProperties = false;
        bool omitParameterName = false;
        ITypeSymbol? providerType = null;
        string? providerMethodName = null;

        foreach (var a in attr.NamedArguments)
        {
            if (a.Key == SkipNullProperties)
            {
                skipNullProperties = (bool)a.Value.Value!;
            }
            else if (a.Key == OmitParameterName)
            {
                omitParameterName = (bool)a.Value.Value!;
            }
        }

        if (attr.ConstructorArguments.Length == 2)
        {
            providerType = attr.ConstructorArguments[0].Value as ITypeSymbol;
            providerMethodName = attr.ConstructorArguments[1].Value as string;
        }

        return (skipNullProperties, omitParameterName, providerType, providerMethodName);
    }
}
