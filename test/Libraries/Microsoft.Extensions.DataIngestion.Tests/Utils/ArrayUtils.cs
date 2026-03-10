// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DataIngestion;

internal static class ArrayUtils
{
    internal static TTo[,] Map<TFrom, TTo>(this TFrom[,] from, Func<TFrom, TTo> mapFunc)
    {
        int rows = from.GetLength(0);
        int cols = from.GetLength(1);
        var to = new TTo[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                to[i, j] = mapFunc(from[i, j]);
            }
        }

        return to;
    }
}
