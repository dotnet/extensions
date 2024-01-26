// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class FormattableTestExtensions
    {
        [LoggerMessage(0, LogLevel.Error, "Method1 {p1}")]
        public static partial void Method1(ILogger logger, Formattable p1);

        [LoggerMessage(1, LogLevel.Error, "Method2")]
        public static partial void Method2(ILogger logger, [LogProperties] ComplexObj p1);

        [LoggerMessage(2, LogLevel.Error, "Method3")]
        public static partial void Method3(ILogger logger, Convertible p1);

        internal struct Formattable : IFormattable
        {
            public readonly string ToString(string? format, IFormatProvider? formatProvider)
            {
                return "Formatted!";
            }
        }

        internal struct Convertible : IConvertible
        {
            public readonly TypeCode GetTypeCode() => throw new NotSupportedException();
            public readonly bool ToBoolean(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly byte ToByte(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly char ToChar(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly DateTime ToDateTime(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly decimal ToDecimal(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly double ToDouble(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly short ToInt16(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly int ToInt32(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly long ToInt64(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly sbyte ToSByte(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly float ToSingle(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly object ToType(Type conversionType, IFormatProvider? provider) => throw new NotSupportedException();
            public readonly ushort ToUInt16(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly uint ToUInt32(IFormatProvider? provider) => throw new NotSupportedException();
            public readonly ulong ToUInt64(IFormatProvider? provider) => throw new NotSupportedException();

            public readonly string ToString(IFormatProvider? provider) => "Converted!";
        }

        internal class ComplexObj
        {
            public Formattable P1 { get; }
            public Convertible P2 { get; }
            public CustomToStringTestClass P3 { get; } = new();
        }
    }
}
