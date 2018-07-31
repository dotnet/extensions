// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public static class RazorDocument
    {
        public static DocumentSelector Selector { get; } = new DocumentSelector(
            new DocumentFilter()
            {
                Pattern = "**/*.razor"
            });
    }
}
