// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Extensions.Http.Telemetry;

namespace Microsoft.Extensions.Telemetry.Internal;

internal sealed class OutgoingRequestContext : IOutgoingRequestContext
{
    private static readonly AsyncLocal<RequestMetadata?> _asyncLocal = new();

    public RequestMetadata? RequestMetadata
    {
        get => _asyncLocal.Value;
        set => _asyncLocal.Value = value;
    }
}
