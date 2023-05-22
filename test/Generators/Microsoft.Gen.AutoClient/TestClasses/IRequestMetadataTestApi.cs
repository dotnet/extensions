// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

namespace TestClasses
{
    [AutoClient("MyClient")]
    public interface IRequestMetadataTestApi
    {
        [Get("/api/users")]
        public Task<string> GetUsersAsync(CancellationToken cancellationToken = default);
    }
}
