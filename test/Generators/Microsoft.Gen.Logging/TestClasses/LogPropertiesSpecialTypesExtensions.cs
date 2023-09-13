// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Numerics;
using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class LogPropertiesSpecialTypesExtensions
    {
        internal class MyProps
        {
            public DateTime P0 { get; set; }
            public DateTimeOffset P1 { get; set; }
            public TimeSpan P2 { get; set; }
            public Guid P3 { get; set; }
            public Version? P4 { get; set; }
            public Uri? P5 { get; set; }
            public IPAddress? P6 { get; set; }
            public EndPoint? P7 { get; set; }
            public IPEndPoint? P8 { get; set; }
            public DnsEndPoint? P9 { get; set; }
            public BigInteger P10 { get; set; }
            public Complex P11 { get; set; }
            public Matrix3x2 P12 { get; set; }
            public Matrix4x4 P13 { get; set; }
            public Plane P14 { get; set; }
            public Quaternion P15 { get; set; }
            public Vector2 P16 { get; set; }
            public Vector3 P17 { get; set; }
            public Vector4 P18 { get; set; }

#if NET6_0_OR_GREATER
            public TimeOnly P19 { get; set; }
            public DateOnly P20 { get; set; }
#endif
        }

        [LoggerMessage(LogLevel.Debug)]
        public static partial void M0(ILogger logger, [LogProperties] MyProps p);
    }
}
