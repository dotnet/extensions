// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

namespace TestClasses
{
    [AutoClient("MyClient")]
    [StaticHeader("X-MyHeader", "MyValue")]
    public interface IStaticHeaderTestClient
    {
        [Get("/api/users")]
        public Task<string> GetUsers(CancellationToken cancellationToken = default);

        [Get("/api/users")]
        [StaticHeader("X-MyHeader1", "MyValue")]
        [StaticHeader("X-MyHeader2", "MyValue")]
        public Task<string> GetUsersHeaders(CancellationToken cancellationToken = default);
    }
}
