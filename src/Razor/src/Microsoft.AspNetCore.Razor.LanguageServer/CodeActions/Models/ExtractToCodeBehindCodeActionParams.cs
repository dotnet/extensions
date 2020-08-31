// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models
{
    internal sealed class ExtractToCodeBehindCodeActionParams
    {
        public DocumentUri Uri { get; set; }
        public int ExtractStart { get; set; }
        public int ExtractEnd { get; set; }
        public int RemoveStart { get; set; }
        public int RemoveEnd { get; set; }
    }
}
