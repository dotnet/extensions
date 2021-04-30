// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Tooltip;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    /// <summary>
    /// VS-specific completion item based off of LSP's VSCompletionItem.
    /// </summary>
    internal class VSCompletionItem : CompletionItem
    {
        public VSClassifiedTextElement Description { get; set; }
    }
}
