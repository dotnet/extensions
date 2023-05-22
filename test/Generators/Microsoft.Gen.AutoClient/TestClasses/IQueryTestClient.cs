// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

namespace TestClasses
{
    [AutoClient("MyClient")]
    public interface IQueryTestClient
    {
        [Get("/api/users")]
        public Task<string> GetUsers([Query] string paramQuery, CancellationToken cancellationToken = default);

        [Get("/api/users")]
        public Task<string> GetUsersCustom([Query("paramQueryCustom")] string paramQuery, CancellationToken cancellationToken = default);

        [Get("/api/users")]
        public Task<string> GetUsers2([Query] string paramQuery1, [Query] string paramQuery2, CancellationToken cancellationToken = default);

        [Get("/api/users")]
        public Task<string> GetUsersObject([Query] CustomObject paramQuery, CancellationToken cancellationToken = default);

        public class CustomObject
        {
            public override string? ToString() => "CustomObjectToString";
        }
    }
}
