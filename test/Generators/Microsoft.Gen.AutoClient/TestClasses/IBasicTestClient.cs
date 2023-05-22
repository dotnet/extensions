// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

namespace TestClasses
{
    [AutoClient("MyClient")]
    public interface IBasicTestClient
    {
        [Get("/api/users")]
        public Task<string> GetUsers(CancellationToken cancellationToken = default);

        [Delete("/api/users")]
        public Task<string> DeleteUsers(CancellationToken cancellationToken = default);

        [Head("/api/users")]
        public Task<string> HeadUsers(CancellationToken cancellationToken = default);

        [Options("/api/users")]
        public Task<string> OptionsUsers(CancellationToken cancellationToken = default);

        [Patch("/api/users")]
        public Task<string> PatchUsers(CancellationToken cancellationToken = default);

        [Post("/api/users")]
        public Task<string> PostUsers(CancellationToken cancellationToken = default);

        [Put("/api/users")]
        public Task<string> PutUsers(CancellationToken cancellationToken = default);

        [Get("/api/users")]
        public Task<string> GetUsersWithCancellationToken(CancellationToken cancellationToken);
    }
}
