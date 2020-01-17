// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Common;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal interface IRazorFileChangeListener
    {
        void RazorFileChanged(string filePath, RazorFileChangeKind kind);
    }
}
