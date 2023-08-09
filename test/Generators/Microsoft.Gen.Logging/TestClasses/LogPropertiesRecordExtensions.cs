// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class LogPropertiesRecordExtensions
    {
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "At symbol testing")]
        internal record class MyRecordClass(int Value, string @class)
        {
            public int GetOnlyValue => Value + 1;
            public decimal @event => Value - 1.0m;
        }

        internal record struct MyRecordStruct(int IntValue, string StringValue)
        {
            public long GetOnlyValue => IntValue + 1L;
        }

        internal readonly record struct MyReadonlyRecordStruct(int IntValue, string StringValue)
        {
            public long GetOnlyValue => IntValue + 1L;
        }

        [LoggerMessage(LogLevel.Debug)]
        public static partial void LogRecordClass(ILogger logger, [LogProperties] MyRecordClass p0);

        [LoggerMessage(LogLevel.Debug, "Struct is: {p0}")]
        public static partial void LogRecordStruct(ILogger logger, [LogProperties] MyRecordStruct p0);

        [LoggerMessage(LogLevel.Debug, "Readonly struct is: {p0}")]
        public static partial void LogReadonlyRecordStruct(ILogger logger, [LogProperties] MyReadonlyRecordStruct p0);
    }
}
