// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

namespace TestClasses
{
    [AutoClient("MyClient")]
    public interface IParamHeaderTestClient
    {
        [Get("/api/users")]
        public Task<string> GetUsers([Header("X-MyHeader")] string? headerValue, CancellationToken cancellationToken = default);

        [Get("/api/users")]
        public Task<string> GetUsersObject([Header("X-MyHeader")] CustomObject headerValue, CancellationToken cancellationToken = default);

        public class CustomObject
        {
            public override string? ToString() => "CustomObjectToString";
        }
    }
}
