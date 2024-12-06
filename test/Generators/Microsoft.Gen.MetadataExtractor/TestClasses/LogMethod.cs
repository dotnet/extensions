// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Test;

public class LogMethod
{
    [LoggerMessage("Hello {user}")]
    public void LogHello([C2(Notes = "Note 3")] string user, int port);
}
