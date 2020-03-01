// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public abstract class RazorLanguageServerCustomMessageTarget
    {
        [JsonRpcMethod(LanguageServerConstants.RazorUpdateCSharpBufferEndpoint)]
        public abstract Task UpdateCSharpBufferAsync(JToken token, CancellationToken cancellationToken);
    }
}
