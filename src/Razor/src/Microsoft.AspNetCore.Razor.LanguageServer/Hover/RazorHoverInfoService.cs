// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using HoverModel = OmniSharp.Extensions.LanguageServer.Protocol.Models.Hover;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Hover
{
    internal abstract class RazorHoverInfoService
    {
        public abstract HoverModel GetHoverInfo(RazorCodeDocument codeDocument, SourceLocation location, ClientCapabilities clientCapabilities);
    }
}
