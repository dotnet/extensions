// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Resilience.Resilience.Internal;

internal sealed class FailureEventMetricsOptions
{
    public Dictionary<Type, Func<object, FailureResultContext>> Factories { get; } = new();
}
