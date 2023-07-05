// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Logging;

#pragma warning disable S2696

internal sealed partial class ExtendedLogger : ILogger
{
    [ThreadStatic]
    private static PropertyJoiner? _joiner;

    [ThreadStatic]
    private static PropertyBag? _bag;

    private static PropertyJoiner Joiner
    {
        get
        {
            var joiner = _joiner;
            if (joiner == null)
            {
                joiner = new();
                _joiner = joiner;
            }

            return joiner;
        }
    }

    private static PropertyBag Bag
    {
        get
        {
            var bag = _bag;
            if (bag == null)
            {
                bag = new();
                _bag = bag;
            }

            return bag;
        }
    }
}
