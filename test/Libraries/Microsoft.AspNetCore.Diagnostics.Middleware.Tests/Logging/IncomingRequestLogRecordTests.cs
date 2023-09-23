// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

public class IncomingRequestLogRecordTests
{
    [Fact]
    public void ShouldInitializeProps()
    {
        // This is kill Stryker's mutants:
        var logRecord = new IncomingRequestLogRecord();
        Assert.Empty(logRecord.Host);
        Assert.Empty(logRecord.Method);
        Assert.Empty(logRecord.Path);
    }
}
