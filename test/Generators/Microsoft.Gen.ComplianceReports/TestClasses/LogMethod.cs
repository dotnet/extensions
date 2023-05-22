// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Telemetry.Logging;

namespace Test;

public class LogMethod
{
    [LogMethod("Hello {user}")]
    public void LogHello([C2(Notes = "Note 3")] string user, int port);
}
