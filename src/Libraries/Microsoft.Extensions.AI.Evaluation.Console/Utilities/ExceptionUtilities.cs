﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.Extensions.AI.Evaluation.Console.Utilities;

internal static class ExceptionUtilities
{
    internal static bool IsCancellation(this Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return true;
        }
        else if (exception is AggregateException aggregateException)
        {
            return aggregateException.InnerExceptions.All(IsCancellation);
        }
        else
        {
            return false;
        }
    }
}
