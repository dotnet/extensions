// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models
{
    internal class SemanticTokensEditCollection
    {
        public string ResultId { get; set; }
        public IReadOnlyList<SemanticTokensEdit> Edits { get; set; }
    }
}
