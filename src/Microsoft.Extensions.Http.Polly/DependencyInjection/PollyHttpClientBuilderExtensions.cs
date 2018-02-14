// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Polly;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions methods for configuring <see cref="PolicyHttpMessageHandler"/> message handlers as part of
    /// and <see cref="HttpClient"/> message handler pipeline.
    /// </summary>
    public static class PollyHttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with the provided
        /// <see cref="IAsyncPolicy"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="policy">The <see cref="IAsyncPolicy"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddPolicyHandler(this IHttpClientBuilder builder, IAsyncPolicy policy)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            // Important - cache policy instances so that they are singletons per handler.
            var innerPolicy = policy.WrapAsync(Policy.NoOpAsync<HttpResponseMessage>());

            builder.AddHttpMessageHandler(() => new PolicyHttpMessageHandler(innerPolicy));
            return builder;
        }

        /// <summary>
        /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with the provided
        /// <see cref="IAsyncPolicy{HttpResponseMessage}"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="policy">The <see cref="IAsyncPolicy{HttpResponseMessage}"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddPolicyHandler(this IHttpClientBuilder builder, IAsyncPolicy<HttpResponseMessage> policy)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            builder.AddHttpMessageHandler(() => new PolicyHttpMessageHandler(policy));
            return builder;
        }

        /// <summary>
        /// Adds a <see cref="PolicyHttpMessageHandler"/> which will surround request execution with a <see cref="Policy"/>
        /// created by executing the provided configuration delegate. The policy builder will be preconfigured to trigger
        /// application of the policy for requests that fail with a connection or server error (5XX status code).
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="configurePolicy">A delegate used to create a <see cref="IAsyncPolicy{HttpResponseMessage}"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// See the remarks on <see cref="PolicyHttpMessageHandler"/> for guidance on configuring policies.
        /// </para>
        /// <para>
        /// The <see cref="PolicyBuilder{HttpResponseMessage}"/> provided to <paramref name="configurePolicy"/> has been
        /// preconfigured to handle connection errors (as <see cref="HttpRequestException"/>) or server errors (as a 5XX HTTP
        /// status code). The configuration is similar to the following code sample:
        /// <code>
        /// Policy.HandleAsync&lt;HttpRequestException&gt;().OrResult&lt;HttpResponseMessage&gt;(response =>
        /// {
        ///     return response.StatusCode >= HttpStatusCode.InternalServerError;
        /// }
        /// </code>
        /// </para>
        /// <para>
        /// The policy created by <paramref name="configurePolicy"/> will be cached indefinitely per named client. Policies
        /// are generally designed to act as singletons, and can be shared when appropriate. To share a policy across multiple
        /// named clients, first create the policy and the pass it to multiple calls to 
        /// <see cref="AddPolicyHandler(IHttpClientBuilder, IAsyncPolicy)"/> or
        /// <see cref="AddPolicyHandler(IHttpClientBuilder, IAsyncPolicy{HttpResponseMessage})"/> as desired.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddServerErrorPolicyHandler(
            this IHttpClientBuilder builder, 
            Func<PolicyBuilder<HttpResponseMessage>, IAsyncPolicy<HttpResponseMessage>> configurePolicy)
        {
            
            var policyBuilder = Policy.Handle<HttpRequestException>().OrResult<HttpResponseMessage>(response =>
            {
                return response.StatusCode >= HttpStatusCode.InternalServerError;
            });

            // Important - cache policy instances so that they are singletons per handler.
            var policy = configurePolicy(policyBuilder);

            builder.AddHttpMessageHandler(() => new PolicyHttpMessageHandler(policy));
            return builder;
        }
    }
}
