// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;

namespace Polly
{
    // This is temporary until this code moves into Polly.
    internal static class PolicyExtensions
    {
        public static PolicyBuilder<HttpResponseMessage> HandleTransientHttpError()
        {
            return Policy.Handle<HttpRequestException>().OrTransientHttpStatusCode();
        }

        public static PolicyBuilder<HttpResponseMessage> OrTransientHttpStatusCode(this PolicyBuilder policyBuilder)
        {
            if (policyBuilder == null)
            {
                throw new ArgumentNullException(nameof(policyBuilder));
            }

            return policyBuilder.OrResult<HttpResponseMessage>(response =>
            {
                return (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout;
            });
        }

        public static PolicyBuilder<HttpResponseMessage> OrTransientHttpError(this PolicyBuilder policyBuilder)
        {
            if (policyBuilder == null)
            {
                throw new ArgumentNullException(nameof(policyBuilder));
            }

            return policyBuilder.Or<HttpRequestException>().OrTransientHttpStatusCode();
        }

        public static PolicyBuilder<HttpResponseMessage> OrTransientHttpStatusCode(this PolicyBuilder<HttpResponseMessage> policyBuilder)
        {
            if (policyBuilder == null)
            {
                throw new ArgumentNullException(nameof(policyBuilder));
            }

            return policyBuilder.OrResult(response =>
            {
                return (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout;
            });
        }

        public static PolicyBuilder<HttpResponseMessage> OrTransientHttpError(this PolicyBuilder<HttpResponseMessage> policyBuilder)
        {
            if (policyBuilder == null)
            {
                throw new ArgumentNullException(nameof(policyBuilder));
            }

            return policyBuilder.Or<HttpRequestException>().OrTransientHttpStatusCode();
        }
    }
}
