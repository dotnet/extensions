// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
#nullable enable
using System;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class ProvideSemanticTokensResponse
    {
        public ProvideSemanticTokensResponse(SemanticTokens result, long? hostDocumentSyncVersion)
        {
            Result = result;
            HostDocumentSyncVersion = hostDocumentSyncVersion;
        }

        public SemanticTokens Result { get; }

        public long? HostDocumentSyncVersion { get; }

        public override bool Equals(object obj) =>
            obj is ProvideSemanticTokensResponse other &&
            other.HostDocumentSyncVersion.Equals(HostDocumentSyncVersion) && other.Result.Equals(Result);

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
#pragma warning restore CS0618
