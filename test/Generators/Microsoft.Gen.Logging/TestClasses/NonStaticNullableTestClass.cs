// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;

namespace TestClasses
{
    public partial class NonStaticNullableTestClass
    {
        private readonly ILogger? _logger;

        public NonStaticNullableTestClass(ILogger? logger)
        {
            _logger = logger;
        }

        [LoggerMessage(2, LogLevel.Debug, "M2 {p0} {p1} {p2}")]
        public partial void M2([PrivateData] string p0, [PrivateData] string p1, [PrivateData] string p2);
    }
}
