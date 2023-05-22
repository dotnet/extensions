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

        LoggingProperty firstProperty = new(
            parameter.NameWithAt,
            parameter.Type,
            null,
            false,
            parameter.IsNullable,
            parameter.IsReference,
            parameter.IsEnumerable,
            false,
            false,
            Array.Empty<LoggingProperty>());

        _ = propertyChain.AddFirst(firstProperty);

        TraverseParameterPropertiesTransitively(propertyChain, parameter.PropertiesToLog, callback);
    }

    private static void TraverseParameterPropertiesTransitively(
        LinkedList<LoggingProperty> propertyChain,
        IReadOnlyCollection<LoggingProperty> propertiesToLog,
        Action<IEnumerable<LoggingProperty>, LoggingProperty> callback)
    {
        foreach (var propertyToLog in propertiesToLog)
        {
            if (propertyToLog.TransitiveMembers.Count > 0)
            {
                _ = propertyChain.AddLast(propertyToLog);
                TraverseParameterPropertiesTransitively(propertyChain, propertyToLog.TransitiveMembers, callback);
                propertyChain.RemoveLast();
            }
            else
            {
                callback(propertyChain, propertyToLog);
            }
        }
    }
}
