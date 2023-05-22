// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.LocalAnalyzers.Utilities;

internal static class OperationExtensions
{
    /// <summary>
    /// Gets the list of ancestor operations up to the specified operation.
    /// </summary>
    /// <param name="operationToStart">Node to start traversing.</param>
    /// <param name="parent">Node to stop traversing.</param>
    /// <returns>The enumerator.</returns>
    public static IEnumerable<IOperation> Ancestors(this IOperation operationToStart, IOperation parent)
    {
        while (operationToStart.Parent != null)
        {
            if (operationToStart.Parent == parent)
            {
                yield break;
            }

            yield return operationToStart.Parent;
            operationToStart = operationToStart.Parent;
        }
    }
}
