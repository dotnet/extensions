// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

namespace TestClasses
{
    [AutoClient("MyClient")]
    public interface IReturnTypesTestClient
    {
        [Get("/api/users")]
        public Task<CustomObject> GetUsers(CancellationToken cancellationToken = default);

        [Get("/api/users")]
        public Task<System.Net.Http.HttpResponseMessage> GetUsersRaw(CancellationToken cancellationToken = default);

        [Get("/api/users")]
        public Task<string> GetUsersTextPlain(CancellationToken cancellationToken = default);

        public class CustomObject
        {
            public string CustomProperty { get; set; } = string.Empty;
        }
    }
}
