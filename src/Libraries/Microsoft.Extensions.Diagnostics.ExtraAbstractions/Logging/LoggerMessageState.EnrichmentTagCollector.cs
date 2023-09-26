// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.Extensions.Logging;

public partial class LoggerMessageState : IEnrichmentTagCollector
{
    /// <inheritdoc/>
    void IEnrichmentTagCollector.Add(string key, object value)
    {
        AddTag(key, value);
    }
}
