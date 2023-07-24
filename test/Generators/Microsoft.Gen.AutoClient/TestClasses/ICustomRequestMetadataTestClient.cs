// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

namespace TestClasses
{
    [AutoClient("MyClient", "MyDependency")]
    public interface ICustomRequestMetadataTestClient
    {
        [Get("/api/user")]
        public Task<string> GetUser(CancellationToken cancellationToken = default);

        [Get("/api/users", RequestName = "MyRequestName")]
        public Task<string> GetUsers(CancellationToken cancellationToken = default);
    }
}
