// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models
{
    internal class ExtendedCodeActionContext : CodeActionContext
    {
        [Optional]
        public Range SelectionRange { get; set; }
    }
}
