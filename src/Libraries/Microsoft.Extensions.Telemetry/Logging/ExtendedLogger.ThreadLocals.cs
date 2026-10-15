// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging;

internal sealed partial class ExtendedLogger : ILogger
{
    // A logging call takes the joiner out of the current thread's slot and puts it back once it's done with it. This way,
    // a logging call made on the same thread while another one is in progress (e.g., by a logger provider) creates a joiner
    // of its own instead of overwriting and clearing the one in use. If a logging call throws before it puts its joiner back,
    // the next call simply creates a new one.
    [ThreadStatic]
    private static ModernTagJoiner? _modernJoiner;

    [ThreadStatic]
    private static LegacyTagJoiner? _legacyJoiner;

    private static ModernTagJoiner RentModernJoiner()
    {
        var joiner = _modernJoiner ?? new();
        _modernJoiner = null;
        return joiner;
    }

    private static void ReturnModernJoiner(ModernTagJoiner joiner)
    {
        joiner.Clear();
        _modernJoiner = joiner;
    }

    private static LegacyTagJoiner RentLegacyJoiner()
    {
        var joiner = _legacyJoiner ?? new();
        _legacyJoiner = null;
        return joiner;
    }

    private static void ReturnLegacyJoiner(LegacyTagJoiner joiner)
    {
        joiner.Clear();
        _legacyJoiner = joiner;
    }
}
