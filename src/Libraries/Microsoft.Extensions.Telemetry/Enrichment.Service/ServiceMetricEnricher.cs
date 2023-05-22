// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Enrichment;

internal sealed class ServiceMetricEnricher : IMetricEnricher
{
    private readonly KeyValuePair<string, string>[] _props;

    public ServiceMetricEnricher(
        IOptions<ServiceMetricEnricherOptions> options,
        IOptions<ApplicationMetadata> metadata)
    {
        var enricherOptions = Throw.IfMemberNull(options, options.Value);
        var applicationMetadata = Throw.IfMemberNull(metadata, metadata.Value);

        _props = Initialize(enricherOptions, applicationMetadata);
    }

    public void Enrich(IEnrichmentPropertyBag enrichmentPropertyBag) => enrichmentPropertyBag.Add(_props);

    private static KeyValuePair<string, string>[] Initialize(ServiceMetricEnricherOptions enricherOptions, ApplicationMetadata applicationMetadata)
    {
        var l = new List<KeyValuePair<string, string>>();

        if (enricherOptions.ApplicationName)
        {
            l.Add(new(ServiceEnricherDimensions.ApplicationName, applicationMetadata.ApplicationName));
        }

        if (enricherOptions.EnvironmentName)
        {
            l.Add(new(ServiceEnricherDimensions.EnvironmentName, applicationMetadata.EnvironmentName));
        }

        if (enricherOptions.DeploymentRing && applicationMetadata.DeploymentRing is not null)
        {
            l.Add(new(ServiceEnricherDimensions.DeploymentRing, applicationMetadata.DeploymentRing));
        }

        if (enricherOptions.BuildVersion && applicationMetadata.BuildVersion is not null)
        {
            l.Add(new(ServiceEnricherDimensions.BuildVersion, applicationMetadata.BuildVersion));
        }

        return l.ToArray();
    }
}
