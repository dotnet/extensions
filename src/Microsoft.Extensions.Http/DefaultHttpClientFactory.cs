// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    // This is just a starter implementation. It's obviously wrong on purpose.
    // The plan is to get a basic sample in first, and improve this along with the sample.
    internal class DefaultHttpClientFactory : HttpClientFactory
    {
        public override HttpClient CreateClient()
        {
            return new HttpClient();
        }
    }
}