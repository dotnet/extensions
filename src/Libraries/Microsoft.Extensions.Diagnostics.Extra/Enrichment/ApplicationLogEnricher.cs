// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

internal sealed class ApplicationLogEnricher : IStaticLogEnricher
{
    private readonly KeyValuePair<string, object>[] _tags;

    public ApplicationLogEnricher(
        IOptions<ApplicationLogEnricherOptions> options,
        IOptions<ApplicationMetadata> metadata)
    {
        var enricherOptions = Throw.IfMemberNull(options, options.Value);
        var applicationMetadata = Throw.IfMemberNull(metadata, metadata.Value);

        _tags = Initialize(enricherOptions, applicationMetadata);
    }

    public void Enrich(IEnrichmentTagCollector collector)
    {
        foreach (var kvp in _tags)
        {
            collector.Add(kvp.Key, kvp.Value);
        }
    }

    private static KeyValuePair<string, object>[] Initialize(ApplicationLogEnricherOptions enricherOptions, ApplicationMetadata applicationMetadata)
    {
        var l = new List<KeyValuePair<string, object>>();

        if (enricherOptions.ApplicationName)
        {
            l.Add(new(ApplicationEnricherTags.ApplicationName, applicationMetadata.ApplicationName));
        }

        if (enricherOptions.EnvironmentName)
        {
            l.Add(new(ApplicationEnricherTags.EnvironmentName, applicationMetadata.EnvironmentName));
        }

        if (enricherOptions.DeploymentRing && applicationMetadata.DeploymentRing is not null)
        {
            l.Add(new(ApplicationEnricherTags.DeploymentRing, applicationMetadata.DeploymentRing));
        }

        if (enricherOptions.BuildVersion && applicationMetadata.BuildVersion is not null)
        {
            l.Add(new(ApplicationEnricherTags.BuildVersion, applicationMetadata.BuildVersion));
        }

        return l.ToArray();
    }
}
