// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal sealed class ExtractToCodeBehindCodeActionParams
    {
        public Uri Uri { get; set; }
        public int ExtractStart { get; set; }
        public int ExtractEnd { get; set; }
        public int RemoveStart { get; set; }
        public int RemoveEnd { get; set; }
    }
}
