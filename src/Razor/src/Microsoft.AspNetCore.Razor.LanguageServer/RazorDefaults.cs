// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public static class RazorDefaults
    {
        public static DocumentSelector Selector { get; } = new DocumentSelector(
            new DocumentFilter()
            {
                Pattern = "**/*.{cshtml,razor}"
            });

        public static RazorConfiguration Configuration { get; } = FallbackRazorConfiguration.MVC_2_1;

        public static string RootNamespace { get; } = null;
    }
}
