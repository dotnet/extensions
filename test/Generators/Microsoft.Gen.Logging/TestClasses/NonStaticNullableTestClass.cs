// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    public partial class NonStaticNullableTestClass
    {
        private readonly ILogger? _logger;
        private readonly IRedactorProvider? _redactorProvider;

        public NonStaticNullableTestClass(ILogger? logger, IRedactorProvider? redactorProvider)
        {
            _logger = logger;
            _redactorProvider = redactorProvider;
        }

        [LogMethod(2, LogLevel.Debug, "M2 {p0} {p1} {p2}")]
        public partial void M2([PrivateData] string p0, [PrivateData] string p1, [PrivateData] string p2);
    }
}
