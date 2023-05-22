// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;

namespace TestClasses
{
    [AutoClient("MyClient")]
    public interface IBodyTestClient
    {
        [Post("/api/users")]
        public Task<string> PostUsers([Body] BodyObject body, CancellationToken cancellationToken = default);

        [Put("/api/users")]
        public Task<string> PutUsers([Body(BodyContentType.TextPlain)] BodyObject body, CancellationToken cancellationToken = default);

        public class BodyObject
        {
            public string Value { get; set; } = "MyBodyObjectValue";

            public override string? ToString() => Value;
        }
    }
}
