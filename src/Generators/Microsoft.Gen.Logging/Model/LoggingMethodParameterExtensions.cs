// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Gen.Logging.Model;

internal static class LoggingMethodParameterExtensions
{
    internal static void TraverseParameterPropertiesTransitively(
        this LoggingMethodParameter parameter,
        Action<IEnumerable<LoggingProperty>, LoggingProperty> callback)
    {
        var propertyChain = new LinkedList<LoggingProperty>();

        var firstProperty = new LoggingProperty
        {
            PropertyName = parameter.ParameterName,
            TagName = parameter.TagName,
            NeedsAtSign = parameter.NeedsAtSign,
            Type = parameter.Type,
            IsNullable = parameter.IsNullable,
            IsReference = parameter.IsReference,
            IsEnumerable = parameter.IsEnumerable
        };

        _ = propertyChain.AddFirst(firstProperty);

        TraverseParameterPropertiesTransitively(propertyChain, parameter.Properties, callback);
    }

    private static void TraverseParameterPropertiesTransitively(
        LinkedList<LoggingProperty> propertyChain,
        IReadOnlyCollection<LoggingProperty> propertiesToLog,
        Action<IEnumerable<LoggingProperty>, LoggingProperty> callback)
    {
        foreach (var propertyToLog in propertiesToLog)
        {
            if (propertyToLog.Properties.Count > 0)
            {
                _ = propertyChain.AddLast(propertyToLog);
                TraverseParameterPropertiesTransitively(propertyChain, propertyToLog.Properties, callback);
                propertyChain.RemoveLast();
            }
            else
            {
                callback(propertyChain, propertyToLog);
            }
        }
    }
}
