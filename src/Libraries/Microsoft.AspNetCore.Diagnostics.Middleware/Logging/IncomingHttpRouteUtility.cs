// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

internal sealed class IncomingHttpRouteUtility : IIncomingHttpRouteUtility
{
    private static readonly Type _dataClassificationAttributeType = typeof(DataClassificationAttribute);
    private readonly ConditionalWeakTable<Endpoint, FrozenDictionary<string, DataClassification>> _parametersToRedactCache = new();

    public IReadOnlyDictionary<string, DataClassification> GetSensitiveParameters(string httpRoute, HttpRequest request, IReadOnlyDictionary<string, DataClassification> defaultSensitiveParameters)
    {
        if (string.IsNullOrEmpty(httpRoute))
        {
            return defaultSensitiveParameters;
        }

        var endpoint = request.HttpContext.GetEndpoint();
        if (endpoint == null)
        {
            return defaultSensitiveParameters;
        }

        if (_parametersToRedactCache.TryGetValue(endpoint, out var result))
        {
            return result;
        }

        var parameters = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.Parameters;
        if (parameters == null)
        {
            return defaultSensitiveParameters;
        }

        var parametersToRedact = new Dictionary<string, DataClassification>();
        foreach (var defaultParameter in defaultSensitiveParameters)
        {
            parametersToRedact.Add(defaultParameter.Key, defaultParameter.Value);
        }

        foreach (var parameter in parameters)
        {
            var p = parameter as ControllerParameterDescriptor;

            if (p != null)
            {
                var dataClassificationAttributes = p.ParameterInfo.GetCustomAttributes(_dataClassificationAttributeType, true);

                if (dataClassificationAttributes.Length > 0)
                {
                    var classification = ((DataClassificationAttribute)dataClassificationAttributes[0]).Classification;
                    parametersToRedact[p.ParameterInfo.Name!] = classification;
                }
            }
        }

        var frozenParametersToRedact = parametersToRedact.ToFrozenDictionary(StringComparer.Ordinal);
        _parametersToRedactCache.AddOrUpdate(endpoint, frozenParametersToRedact);
        return frozenParametersToRedact;
    }
}
