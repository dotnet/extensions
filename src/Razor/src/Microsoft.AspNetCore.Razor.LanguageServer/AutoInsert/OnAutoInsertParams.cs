// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MediatR;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert
{
    internal class OnAutoInsertParams : ITextDocumentIdentifierParams, IRequest<OnAutoInsertResponse>, IBaseRequest
    {
        public TextDocumentIdentifier TextDocument { get; set; }

        public Position Position { get; set; }

        [JsonProperty("ch")]
        public string Character { get; set; }

        public FormattingOptions Options { get; set; }
    }
}
