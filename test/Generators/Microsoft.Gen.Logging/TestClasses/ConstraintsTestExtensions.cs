// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable S1186 // Methods should not be empty

namespace TestClasses
{
    internal static partial class ConstraintsTestExtensions<T>
        where T : class
    {
        [LogMethod(0, LogLevel.Debug, "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T _)
        {
        }
    }

    internal static partial class ConstraintsTestExtensions1<T>
        where T : struct
    {
        [LogMethod(0, LogLevel.Debug, "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T _)
        {
        }
    }

    internal static partial class ConstraintsTestExtensions2<T>
        where T : unmanaged
    {
        [LogMethod(0, LogLevel.Debug, "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T _)
        {
        }
    }

    internal static partial class ConstraintsTestExtensions3<T>
        where T : new()
    {
        [LogMethod(0, LogLevel.Debug, "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T _)
        {
        }
    }

    internal static partial class ConstraintsTestExtensions4<T>
        where T : Attribute
    {
        [LogMethod(0, LogLevel.Debug, "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T _)
        {
        }
    }

    internal static partial class ConstraintsTestExtensions5<T>
        where T : notnull
    {
        [LogMethod(0, LogLevel.Debug, "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T _)
        {
        }
    }

    internal static partial class ConstraintsTestExtensionsMultiple<T>
        where T : class, new()
    {
        [LogMethod(0, LogLevel.Debug, "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        public static void Foo(T _)
        {
        }
    }
}
