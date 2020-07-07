// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal sealed class RazorCodeActionContext
    {
        public RazorCodeActionContext(CodeActionParams request, RazorCodeDocument document, SourceLocation location)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Location = location;
        }

        public CodeActionParams Request { get; }
        public RazorCodeDocument Document { get; }
        public SourceLocation Location { get; }
    }
}
