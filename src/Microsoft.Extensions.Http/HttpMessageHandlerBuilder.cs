// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    /// <summary>
    /// A builder abstraction for configuring <see cref="HttpMessageHandler"/> instances.
    /// </summary>
    /// <remarks>
    /// The <see cref="HttpMessageHandlerBuilder"/> is registered in the service collection as
    /// a transient service. Callers should retrieve a new instance for each <see cref="HttpMessageHandler"/> to
    /// be created. Implementors should expect each instance to be used a single time.
    /// </remarks>
    public abstract class HttpMessageHandlerBuilder
    {
        /// <summary>
        /// Gets or sets the primary <see cref="HttpMessageHandler"/>.
        /// </summary>
        public abstract HttpMessageHandler PrimaryHandler { get; set; }

        /// <summary>
        /// Gets a list of additional <see cref="DelegatingHandler"/> instances used to configure an
        /// <see cref="HttpClient"/> pipeline.
        /// </summary>
        public abstract IList<DelegatingHandler> AdditionalHandlers { get; }

        /// <summary>
        /// Creates an <see cref="HttpMessageHandler"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="HttpMessageHandler"/> built from the <see cref="PrimaryHandler"/> and
        /// <see cref="AdditionalHandlers"/>.
        /// </returns>
        public abstract HttpMessageHandler Build();
    }
}
