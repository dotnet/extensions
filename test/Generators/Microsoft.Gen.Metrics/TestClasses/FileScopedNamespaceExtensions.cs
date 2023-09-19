// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace TestClasses;

internal static partial class FileScopedExtensions
{
    [Counter]
    public static partial FileScopedNamespaceCounter CreateCounter(Meter meter);

    [Counter<double>]
    public static partial FileScopedNamespaceGenericDoubleCounter CreateGenericDoubleCounter(Meter meter);
}
