// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.AI.Evaluation.Console.Utilities;

internal static class ExceptionUtilities
{
    internal static bool IsCancellation(this Exception exception) =>
        exception switch
        {
            OperationCanceledException => true,
            AggregateException aggregateException => aggregateException.ContainsOnlyCancellations(),
            _ => false
        };

    private static bool ContainsOnlyCancellations(this AggregateException exception)
    {
        var exceptionsToCheck = new Stack<Exception>();
        exceptionsToCheck.Push(exception);

        bool containsAtLeastOneCancellation = false;
        while (exceptionsToCheck.TryPop(out Exception? current))
        {
            if (current is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    exceptionsToCheck.Push(innerException);
                }
            }
            else if (current is OperationCanceledException)
            {
                containsAtLeastOneCancellation = true;
            }
            else
            {
                return false;
            }
        }

        return containsAtLeastOneCancellation;
    }
}
