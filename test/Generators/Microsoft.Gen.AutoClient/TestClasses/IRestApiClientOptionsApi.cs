// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

namespace TestClasses
{
    [AutoClient("MyClient")]
    public interface IRestApiClientOptionsApi
    {
        [Post("/api/dict")]
        public Task<string> PostDictionary([Body] Dictionary<string, string> body, CancellationToken cancellationToken = default);
    }
}
