// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

#pragma warning disable IDE1006

namespace TestClasses
{
    [AutoClient("MyClient")]
    public interface INameConflictTestClient
    {
        [Get("/api/users")]
        public Task<string> Statics(CancellationToken cancellationToken);

        [Get("/api/users")]
        public Task<string> _httpClient(CancellationToken cancellationToken);

        [Get("/api/users")]
        public Task<string> _autoClientOptions(CancellationToken cancellationToken);

        [Get("/api/users")]
        public Task<string> GetUsers1([Header("X-MyHeader")] string? httpRequestMessage, CancellationToken cancellationToken = default);

        [Get("/api/users")]
        public Task<string> GetUsers2([Header("X-MyHeader")] string? _autoClientOptions, CancellationToken cancellationToken = default);

        [Get("/api/users")]
        public Task<string> GetUsers3([Header("X-MyHeader")] string? _httpClient, CancellationToken cancellationToken = default);

        [Get("/api/users")]
        public Task<string> GetUsers4([Header("X-MyHeader")] string? Statics, CancellationToken cancellationToken = default);
    }
}
