// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IHttpMessageHandlerBuilderFilter[] _filters;

        // Lazy, because we're using a subtle pattern here to ensure that only one instance of
        // HttpMessageHandler is created for each name.
        private readonly ConcurrentDictionary<string, Lazy<HttpMessageHandler>> _cache;
        private readonly Func<string, Lazy<HttpMessageHandler>> _valueFactory;

        public DefaultHttpClientFactory(
            IServiceProvider services,
            IOptionsMonitor<HttpClientFactoryOptions> optionsMonitor,
            IEnumerable<IHttpMessageHandlerBuilderFilter> filters)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (optionsMonitor == null)
            {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            if (filters ==null)
            {
                throw new ArgumentNullException(nameof(filters));
            }

            _services = services;
            _optionsMonitor = optionsMonitor;
            _filters = filters.ToArray();

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

        // Internal for tests
        internal HttpMessageHandler CreateHandler(string name)
        {
            var builder = _services.GetRequiredService<HttpMessageHandlerBuilder>();
            builder.Name = name;

            // This is similar to the initialization pattern in:
            // https://github.com/aspnet/Hosting/blob/e892ed8bbdcd25a0dafc1850033398dc57f65fe1/src/Microsoft.AspNetCore.Hosting/Internal/WebHost.cs#L188
            Action<HttpMessageHandlerBuilder> configure = Configure;
            for (var i = _filters.Length -1; i >= 0; i--)
            {
                configure = _filters[i].Configure(configure);
            }

            configure(builder);

            return builder.Build();

            void Configure(HttpMessageHandlerBuilder b)
            {
                var options = _optionsMonitor.Get(name);
                for (var i = 0; i < options.HttpMessageHandlerBuilderActions.Count; i++)
                {
                    options.HttpMessageHandlerBuilderActions[i](b);
                }
            }
        }
    }
}