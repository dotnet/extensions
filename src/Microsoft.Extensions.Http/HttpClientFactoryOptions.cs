// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    /// <summary>
    /// An options class for configuring the default <see cref="IHttpClientFactory"/>.
    /// </summary>
    public class HttpClientFactoryOptions
    {
        /// <summary>
        /// Gets a list of operations used to configure an <see cref="HttpMessageHandlerBuilder"/>.
        /// </summary>
        public IList<Action<HttpMessageHandlerBuilder>> HttpMessageHandlerBuilderActions { get; } = new List<Action<HttpMessageHandlerBuilder>>();

        /// <summary>
        /// Gets a list of operations used to configure an <see cref="HttpClient"/>.
        /// </summary>
        public IList<Action<HttpClient>> HttpClientActions { get; } = new List<Action<HttpClient>>();
    }
}
