// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging;

internal sealed partial class ExtendedLogger : ILogger
{
    [field: ThreadStatic]
    private static ModernTagJoiner ModernJoiner => field ??= new();

    [field: ThreadStatic]
    private static LegacyTagJoiner LegacyJoiner => field ??= new();
}
