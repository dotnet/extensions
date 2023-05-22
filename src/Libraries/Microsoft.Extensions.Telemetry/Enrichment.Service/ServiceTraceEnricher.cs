// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Enrichment;

internal sealed class ServiceTraceEnricher : ITraceEnricher
{
    private readonly string? _envName;
    private readonly string? _appName;
    private readonly string? _buildVersion;
    private readonly string? _deploymentRing;

    public ServiceTraceEnricher(
        IOptions<ServiceTraceEnricherOptions> options,
        IOptions<ApplicationMetadata> metadata)
    {
        var enricherOptions = options.Value;
        var applicationMetadata = metadata.Value;

        if (enricherOptions.ApplicationName)
        {
            _appName = applicationMetadata.ApplicationName;
        }

        if (enricherOptions.EnvironmentName)
        {
            _envName = applicationMetadata.EnvironmentName;
        }

        if (enricherOptions.DeploymentRing)
        {
            _deploymentRing = applicationMetadata.DeploymentRing;
        }

        if (enricherOptions.BuildVersion)
        {
            _buildVersion = applicationMetadata.BuildVersion;
        }
    }

    public void Enrich(Activity activity)
    {
        if (_appName is not null)
        {
            _ = activity.AddTag(ServiceEnricherDimensions.ApplicationName, _appName);
        }

        if (_envName is not null)
        {
            _ = activity.AddTag(ServiceEnricherDimensions.EnvironmentName, _envName);
        }

        if (_buildVersion is not null)
        {
            _ = activity.AddTag(ServiceEnricherDimensions.BuildVersion, _buildVersion);
        }

        if (_deploymentRing is not null)
        {
            _ = activity.AddTag(ServiceEnricherDimensions.DeploymentRing, _deploymentRing);
        }
    }

    public void EnrichOnActivityStart(Activity activity)
    {
        // nothing
    }
}
