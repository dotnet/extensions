// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Resilience.FaultInjection;

internal sealed class FaultInjectionCustomResultOptions
{
    public object CustomResult { get; set; } = new();
}
