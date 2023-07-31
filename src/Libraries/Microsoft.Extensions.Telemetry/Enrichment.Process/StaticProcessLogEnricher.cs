// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Text;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Enriches logs with process information.
/// </summary>
internal sealed class StaticProcessLogEnricher : IStaticLogEnricher
{
    private readonly string? _processId;

    public StaticProcessLogEnricher(IOptions<ProcessLogEnricherOptions> options)
    {
        var enricherOptions = Throw.IfMemberNull(options, options.Value);

        if (enricherOptions.ProcessId)
        {
#if NET5_0_OR_GREATER
            var pid = Environment.ProcessId;
#else
            var pid = System.Diagnostics.Process.GetCurrentProcess().Id;
#endif

            _processId = pid.ToInvariantString();
        }
    }

    public void Enrich(IEnrichmentPropertyBag enrichmentBag)
    {
        if (_processId != null)
        {
            enrichmentBag.Add(ProcessEnricherDimensions.ProcessId, _processId);
        }
    }
}
