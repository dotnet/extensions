// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;
using Xunit;
using MSOPtions = Microsoft.Extensions.Options.Options;

namespace Microsoft.Extensions.Telemetry.Console.Test;

public class LoggingConsoleExporterTests
{
    [Fact]
    public void Ctor_GivenInvalidArguments_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() => new LoggingConsoleExporter(null!));
        Assert.Throws<ArgumentException>(() => new LoggingConsoleExporter(MSOPtions.Create((LoggingConsoleOptions)null!)));
    }
}
#endif
