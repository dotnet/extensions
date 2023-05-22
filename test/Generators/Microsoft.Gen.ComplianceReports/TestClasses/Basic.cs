// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Telemetry.Logging;

namespace Test;

interface IFoo
{
    [C4]
    public int P0 { get; }
}

[C1]
public class Basic : IFoo
{
    [C2(Notes = "Note 1")]
    public int F0;

    [C2(Notes = null!)]
    public int F1;

    [C3(Notes = "Note 2")]
    public int P0 { get; }

    [C3]
    public int P1 { get; }

    [LogMethod("Hello {user}")]
    public void LogHello([C2(Notes = "Note 3")] string user, int port);

    [LogMethod("World {user}")]
    public void LogWorld([C2] string user, int port);
}
