// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Gen.Metering.Model;

internal enum InstrumentKind
{
    None = 0,
    Counter = 1,
    Histogram = 2,
    Gauge = 3,
    CounterT = 4,
    HistogramT = 5,
}
