// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Resilience.Internal;
internal sealed class OnChangeListenersHandler : IOnChangeListenersHandler
{
    private readonly ConcurrentDictionary<(Type optionsType, string optionsName), IDisposable> _listenersByType = new();
    private readonly IServiceProvider _serviceProvider;

    public OnChangeListenersHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool TryCaptureOnChange<TOptions>(string optionsName)
    {
        var optionsMonitor = _serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>();
        var key = (typeof(TOptions), optionsName);

        if (!_listenersByType.ContainsKey(key))
        {
            var listener = optionsMonitor.OnChange((_, name) =>
            {
                if (name == optionsName)
                {
                    var logger = _serviceProvider.GetRequiredService<ILogger<TOptions>>();
                    logger.PolicyInPipelineUpdated(optionsName);
                }
            });

            if (listener != null)
            {
                _ = _listenersByType.TryAdd(key, listener);

                return true;
            }
        }

        return false;
    }

    public void Dispose()
    {
        foreach (var listenerByType in _listenersByType)
        {
            listenerByType.Value.Dispose();
        }

        _listenersByType.Clear();
    }
}
