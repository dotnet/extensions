// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http
{
    internal class DefaultHttpClientFactory : IHttpClientFactory
    {
        private readonly IServiceProvider _services;
        private readonly IOptionsMonitor<HttpClientFactoryOptions> _optionsMonitor;

        // Lazy, because we're using a subtle pattern here to ensure that only one instance of
        // HttpMessageHandler is created for each name.
        private readonly ConcurrentDictionary<string, Lazy<HttpMessageHandler>> _cache;
        private readonly Func<string, Lazy<HttpMessageHandler>> _valueFactory;

        public DefaultHttpClientFactory(
            IServiceProvider services,
            IOptionsMonitor<HttpClientFactoryOptions> optionsMonitor)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (optionsMonitor == null)
            {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            _services = services;
            _optionsMonitor = optionsMonitor;

            // case-sensitive because named options is.
            _cache = new ConcurrentDictionary<string, Lazy<HttpMessageHandler>>(StringComparer.Ordinal);
            _valueFactory = (name) => new Lazy<HttpMessageHandler>(() => CreateHandler(name), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public HttpClient CreateClient(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var handler = _cache.GetOrAdd(name, _valueFactory);
            var client = new HttpClient(handler.Value, disposeHandler: false);

            var options = _optionsMonitor.Get(name);
            for (var i = 0; i < options.HttpClientActions.Count; i++)
            {
                options.HttpClientActions[i](client);
            }

            return client;
        }

        private HttpMessageHandler CreateHandler(string name)
        {
            var builder = _services.GetRequiredService<HttpMessageHandlerBuilder>();

            var options = _optionsMonitor.Get(name);
            for (var i = 0; i < options.HandlerBuilderActions.Count; i++)
            {
                options.HandlerBuilderActions[i](builder);
            }

            return builder.Build();
        }
    }
}