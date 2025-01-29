// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Test;

interface IBar
{
    [C4]
    public int P0 { get; }
}

public record RecordProperty([C2] string F0, string F1, [C3] int F2) : IBar
{
    [C2(Notes = "Note 1")]
    public int F3;

    [C2(Notes = null!)]
    public int F4;

    [C3(Notes = "Note 3")]
    public int P0 { get; };

    [C3]
    public int P1 { get; };

    [LoggerMessage("Hello {user}")]
    public void LogHello([C3(Notes = "Note 3")] string user, int port);

    [LoggerMessage("World {user}")]
    public void LogWorld([C2] string user, int port);
}

[C1]
public record DerivedRecordProperty : RecordProperty
{
    [C2(Notes = "Note 2")]
    public override int P0 { get; };
}
