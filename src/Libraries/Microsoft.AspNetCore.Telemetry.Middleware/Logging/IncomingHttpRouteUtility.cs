// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
#endif
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.AspNetCore.Mvc.Controllers;
#endif
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

internal sealed class IncomingHttpRouteUtility : IIncomingHttpRouteUtility
{
#if NETCOREAPP3_1_OR_GREATER
    private static readonly Type _dataClassificationAttributeType = typeof(DataClassificationAttribute);
    private readonly ConcurrentDictionary<string, FrozenDictionary<string, DataClassification>> _parametersToRedactCache = new();

    public IReadOnlyDictionary<string, DataClassification> GetSensitiveParameters(string httpRoute, HttpRequest request, IReadOnlyDictionary<string, DataClassification> defaultSensitiveParameters)
    {
        if (string.IsNullOrEmpty(httpRoute))
        {
            return defaultSensitiveParameters;
        }

        if (_parametersToRedactCache.TryGetValue(httpRoute, out var result))
        {
            return result;
        }

        var parametersToRedact = new Dictionary<string, DataClassification>();
        foreach (var defaultParameter in defaultSensitiveParameters)
        {
            parametersToRedact.Add(defaultParameter.Key, defaultParameter.Value);
        }

        var endpoint = request.HttpContext.GetEndpoint();
        var parameters = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>()?.Parameters;
        if (parameters != null)
        {
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
        }

        return _parametersToRedactCache.GetOrAdd(httpRoute, static (_, paramsToRedact) => paramsToRedact.ToFrozenDictionary(StringComparer.Ordinal), parametersToRedact);
    }
#else
    public IReadOnlyDictionary<string, DataClassification> GetSensitiveParameters(string httpRoute, HttpRequest request, IReadOnlyDictionary<string, DataClassification> defaultSensitiveParameters)
    {
        return defaultSensitiveParameters;
    }
#endif
}
