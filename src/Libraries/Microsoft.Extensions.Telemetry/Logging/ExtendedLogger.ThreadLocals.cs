// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Logging;

#pragma warning disable S2696

internal sealed partial class ExtendedLogger : ILogger
{
    [ThreadStatic]
    private static ModernPropertyJoiner? _modernJoiner;

    [ThreadStatic]
    private static LegacyPropertyJoiner? _legacyJoiner;

    private static ModernPropertyJoiner ModernJoiner
    {
        get
        {
            var joiner = _modernJoiner;
            if (joiner == null)
            {
                joiner = new();
                _modernJoiner = joiner;
            }

            return joiner;
        }
    }

    private static LegacyPropertyJoiner LegacyJoiner
    {
        get
        {
            var joiner = _legacyJoiner;
            if (joiner == null)
            {
                joiner = new();
                _legacyJoiner = joiner;
            }

            return joiner;
        }
    }
}
