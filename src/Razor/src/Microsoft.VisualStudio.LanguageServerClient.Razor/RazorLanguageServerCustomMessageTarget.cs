// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal abstract class RazorLanguageServerCustomMessageTarget
    {
        // Called by the Razor Language Server to update the contents of the virtual CSharp buffer.
        [JsonRpcMethod(LanguageServerConstants.RazorUpdateCSharpBufferEndpoint, UseSingleObjectParameterDeserialization = true)]
        public abstract Task UpdateCSharpBufferAsync(UpdateBufferRequest token, CancellationToken cancellationToken);

        // Called by the Razor Language Server to update the contents of the virtual Html buffer.
        [JsonRpcMethod(LanguageServerConstants.RazorUpdateHtmlBufferEndpoint, UseSingleObjectParameterDeserialization = true)]
        public abstract Task UpdateHtmlBufferAsync(UpdateBufferRequest token, CancellationToken cancellationToken);

        // Called by the Razor Language Server to invoke a textDocument/rangeFormatting request
        // on the virtual Html/CSharp buffer.
        [JsonRpcMethod(LanguageServerConstants.RazorRangeFormattingEndpoint, UseSingleObjectParameterDeserialization = true)]
        public abstract Task<RazorDocumentRangeFormattingResponse> RazorRangeFormattingAsync(RazorDocumentRangeFormattingParams token, CancellationToken cancellationToken);

        // Called by the Razor Language Server to provide code actions from the platform.
        [JsonRpcMethod(LanguageServerConstants.RazorProvideCodeActionsEndpoint, UseSingleObjectParameterDeserialization = true)]
        public abstract Task<VSCodeAction[]> ProvideCodeActionsAsync(CodeActionParams codeActionParams, CancellationToken cancellationToken);

        // Called by the Razor Language Server to resolve code actions from the platform.
        [JsonRpcMethod(LanguageServerConstants.RazorResolveCodeActionsEndpoint, UseSingleObjectParameterDeserialization = true)]
        public abstract Task<VSCodeAction> ResolveCodeActionsAsync(VSCodeAction codeAction, CancellationToken cancellationToken);

        // Called by the Razor Language Server to provide semantic tokens from the platform.
        [JsonRpcMethod(LanguageServerConstants.RazorProvideSemanticTokensEndpoint, UseSingleObjectParameterDeserialization = true)]
        public abstract Task<SemanticTokens> ProvideSemanticTokensAsync(SemanticTokensParams semanticTokensParams, CancellationToken cancellationToken);
    }
}
