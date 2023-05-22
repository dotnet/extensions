// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging.Internals;

public interface IStubRoutingService
{
    Uri Route { get; }
}
