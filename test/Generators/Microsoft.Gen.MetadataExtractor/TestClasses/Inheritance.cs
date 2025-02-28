// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Test;

public class Base
{
    [C1]
    public int P0 { get; }

    [C2]
    public virtual int P1 { get; }
}

public class Inherited : Base
{
    [C3]
    public override int P1 { get; }
}
