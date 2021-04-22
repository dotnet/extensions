// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models
{
    /// <summary>
    /// Used to transport edit information from the Razor client to Razor server.
    /// We avoid using O#'s pre-existing SemanticTokensEdit since it uses ImmutableArrays
    /// and we don't want to deal with the overhead of creating those.
    /// </summary>
    internal record RazorSemanticTokensEdit(int Start, int DeleteCount, int[]? Data);
}
