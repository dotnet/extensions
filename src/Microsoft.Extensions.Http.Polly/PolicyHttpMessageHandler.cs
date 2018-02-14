// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Microsoft.Extensions.Http
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> implementation that executes request processing surrounded by a <see cref="Policy"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This message handler implementation supports the use of policies provided by the Polly library for 
    /// transient-fault-handling and resiliency.
    /// </para>
    /// <para>
    /// The documentation provided here is focused guidance for using Polly together with the <see cref="IHttpClientFactory"/>. 
    /// See the Polly project and its documentation (https://github.com/app-vnext/Polly) for authoritative information on Polly.
    /// </para>
    /// <para>
    /// The extension methods on <see cref="PollyHttpClientBuilderExtensions"/> are designed as a convenient and correct
    /// to creating a <see cref="PolicyHttpMessageHandler"/>.
    /// </para>
    /// <para>
    /// The <see cref="PollyHttpClientBuilderExtensions.AddPolicyHandler(IHttpClientBuilder, IAsyncPolicy)"/> and 
    /// <see cref="PollyHttpClientBuilderExtensions.AddPolicyHandler(IHttpClientBuilder, IAsyncPolicy{HttpResponseMessage})"/>
    /// methods support the creation of a <see cref="PolicyHttpMessageHandler"/> for any kind of policy. This includes
    /// non-reactive policies such as Timeout, or Cache which don't require the underlying request to fail first.
    /// </para>
    /// <para>
    /// The <see cref="PollyHttpClientBuilderExtensions.AddServerErrorPolicyHandler(IHttpClientBuilder, Func{PolicyBuilder{HttpResponseMessage}, IAsyncPolicy{HttpResponseMessage}})"/>
    /// method is an opinionated convenience method that supports the application of a policy for requests that fail due
    /// to a connection failure or server error (5XX HTTP status code). This kind of method supports only reactive policies
    /// such as Retry, Circuit-Breaker or Fallback. This method is only provided for convenience, we recommend creating
    /// your own policies as needed if this does not meet your requirements.
    /// </para>
    /// <para>
    /// Take care when using policies such as Retry or Timeout together as HttpClient provides its own timeout via 
    /// <see cref="HttpClient.Timeout"/>. 
    /// </para>
    /// <para>
    /// All policies provided by Polly are designed to efficient when used in a long-lived way. Certain policies such as the 
    /// Bulkhead and Circuit-Breaker share state and may not have the desired effect when not shared properly. Take care to
    /// ensure the correct lifetimes when using policies and message handlers together in custom scenarios. The extension
    /// methods provided by <see cref="PollyHttpClientBuilderExtensions"/> are designed to assign a long lifetime to policies
    /// and ensure that they can be used when the handler rotation feature is active.
    /// </para>
    /// </remarks>
    public class PolicyHttpMessageHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        /// <summary>
        /// Creates a new <see cref="PolicyHttpMessageHandler"/>.
        /// </summary>
        /// <param name="policy">The policy.</param>
        public PolicyHttpMessageHandler(IAsyncPolicy<HttpResponseMessage> policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            _policy = policy;
        }
        
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return _policy.ExecuteAsync((ct) => base.SendAsync(request, ct), cancellationToken);
        }
    }
}
