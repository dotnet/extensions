// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal abstract class RazorLanguageServerCustomMessageTarget
    {
        // Called by the Razor Language Server to update the contents of the virtual CSharp buffer.
        [JsonRpcMethod(LanguageServerConstants.RazorUpdateCSharpBufferEndpoint)]
        public abstract Task UpdateCSharpBufferAsync(JToken token, CancellationToken cancellationToken);

        // Called by the Razor Language Server to update the contents of the virtual Html buffer.
        [JsonRpcMethod(LanguageServerConstants.RazorUpdateHtmlBufferEndpoint)]
        public abstract Task UpdateHtmlBufferAsync(JToken token, CancellationToken cancellationToken);

        // Called by the Razor Language Server to invoke a textDocument/rangeFormatting request
        // on the virtual Html/CSharp buffer.
        [JsonRpcMethod(LanguageServerConstants.RazorRangeFormattingEndpoint, UseSingleObjectParameterDeserialization = true)]
        public abstract Task<RazorDocumentRangeFormattingResponse> RazorRangeFormattingAsync(RazorDocumentRangeFormattingParams token, CancellationToken cancellationToken);
    }
}
