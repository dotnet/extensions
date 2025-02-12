// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Diagnostics.Sampling;

internal sealed class TraceBasedSampler : LoggingSampler
{
    public override bool ShouldSample<TState>(in LogEntry<TState> _) =>
        Activity.Current?.Recorded ?? true;
}
