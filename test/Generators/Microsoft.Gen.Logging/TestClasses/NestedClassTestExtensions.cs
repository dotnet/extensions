// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0065 // Misplaced using directive

namespace TestClasses
{
    using Alien;

    internal static partial class NestedClassTestExtensions<T>
        where T : Abc
    {
        internal static partial class NestedMiddleParentClass
        {
            internal static partial class NestedClass
            {
                [LoggerMessage(8, LogLevel.Error, "M8")]
                public static partial void M8(ILogger logger);
            }
        }

        public static T Foo(T x) => x;
    }

    internal sealed partial class NonStaticNestedClassTestExtensions<T>
        where T : Abc
    {
        internal partial class NonStaticNestedMiddleParentClass
        {
            internal static partial class NestedClass
            {
                [LoggerMessage(9, LogLevel.Debug, "M9")]
                public static partial void M9(ILogger logger);
            }

            public int Bar() => 42;
        }

        public static T Foo(T x) => x;
    }

    public partial struct NestedStruct
    {
        internal static partial class Logger
        {
            [LoggerMessage(10, LogLevel.Debug, "M10")]
            public static partial void M10(ILogger logger);
        }
    }

    public partial record NestedRecord(string Name, string Address)
    {
        internal static partial class Logger
        {
            [LoggerMessage(11, LogLevel.Debug, "M11")]
            public static partial void M11(ILogger logger);
        }
    }

    public static partial class MultiLevelNestedClass
    {
        public partial struct NestedStruct
        {
            internal partial record NestedRecord(string Name, string Address)
            {
                internal static partial class Logger
                {
                    [LoggerMessage(12, LogLevel.Debug, "M12")]
                    public static partial void M12(ILogger logger);
                }
            }
        }
    }
}

#pragma warning disable SA1403

namespace Alien
{
    public class Abc
    {
    }
}
