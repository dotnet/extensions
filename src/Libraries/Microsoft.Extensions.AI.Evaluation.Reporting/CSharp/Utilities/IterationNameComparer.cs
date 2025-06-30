// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Utilities;

internal static class IterationNameComparer
{
    internal static Comparer<string> Default { get; } =
         Comparer<string>.Create(
             (first, second) =>
             {
                 if (int.TryParse(first, out int firstInteger) &&
                     int.TryParse(second, out int secondInteger))
                 {
                     return firstInteger.CompareTo(secondInteger);
                 }
                 else if (
                     double.TryParse(first, out double firstDouble) &&
                     double.TryParse(second, out double secondDouble))
                 {
                     return firstDouble.CompareTo(secondDouble);
                 }
                 else
                 {
                     return string.Compare(first, second, StringComparison.Ordinal);
                 }
             });
}
