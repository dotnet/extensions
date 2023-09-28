// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.Logging.Parsing;

internal static class AttributeProcessors
{
    private const string MessageProperty = "Message";
    private const string EventNameProperty = "EventName";
    private const string EventIdProperty = "EventId";
    private const string LevelProperty = "Level";
    private const string SkipEnabledCheckProperty = "SkipEnabledCheck";
    private const string SkipNullProperties = "SkipNullProperties";
    private const string OmitReferenceName = "OmitReferenceName";

    private const int LogLevelError = 4;
    private const int LogLevelCritical = 5;

    public static (int? eventId, int? level, string message, string? eventName, bool skipEnabledCheck)
        ExtractLoggerMessageAttributeValues(AttributeData attr, SymbolHolder symbols)
    {
        // Five constructor arg shapes:
        //
        //   ()
        //   (int eventId, LogLevel level, string message)
        //   (LogLevel level, string message)
        //   (LogLevel level)
        //   (string message)

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
            else if (a.Type?.SpecialType == SpecialType.System_Int32)
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
            var v = a.Value.Value;
            if (v != null)
            {
                switch (a.Key)
                {
                    case MessageProperty:
                        if (v is string m)
                        {
                            message = m;
                        }

                        break;

                    case EventNameProperty:
                        if (v is string e)
                        {
                            eventName = e;
                        }

                        break;

                    case LevelProperty:
                        if (v is int l)
                        {
                            level = l;
                        }

                        break;

                    case EventIdProperty:
                        if (v is int id)
                        {
                            eventId = id;
                        }

                        break;

                    case SkipEnabledCheckProperty:
                        if (v is bool b)
                        {
                            skipEnabledCheck = b;
                            useDefaultForSkipEnabledCheck = false;
                        }

                        break;
                }
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

    public static (bool skipNullProperties, bool omitReferenceName) ExtractLogPropertiesAttributeValues(AttributeData attr)
    {
        bool skipNullProperties = false;
        bool omitReferenceName = false;

        foreach (var a in attr.NamedArguments)
        {
            var v = a.Value.Value;
            if (v != null)
            {
                if (a.Key == SkipNullProperties)
                {
                    if (v is bool b)
                    {
                        skipNullProperties = b;
                    }
                }
                else if (a.Key == OmitReferenceName)
                {
                    if (v is bool b)
                    {
                        omitReferenceName = b;
                    }
                }
            }
        }

        return (skipNullProperties, omitReferenceName);
    }

    public static (bool omitReferenceName, ITypeSymbol providerType, string providerMethodName) ExtractTagProviderAttributeValues(AttributeData attr)
    {
        bool omitReferenceName = false;
        ITypeSymbol? providerType = null;
        string? providerMethodName = null;

        foreach (var a in attr.NamedArguments)
        {
            var v = a.Value.Value;
            if (v != null)
            {
                if (a.Key == OmitReferenceName)
                {
                    if (v is bool b)
                    {
                        omitReferenceName = b;
                    }
                }
            }
        }

        providerType = attr.ConstructorArguments[0].Value as ITypeSymbol;
        providerMethodName = attr.ConstructorArguments[1].Value as string;

        return (omitReferenceName, providerType!, providerMethodName!);
    }
}
