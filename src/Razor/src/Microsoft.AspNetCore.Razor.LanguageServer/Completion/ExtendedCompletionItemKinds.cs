// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal enum ExtendedCompletionItemKinds
    {
        None = 0,
        Text = CompletionItemKind.Text,
        Method = CompletionItemKind.Method,
        Function = CompletionItemKind.Function,
        Constructor = CompletionItemKind.Constructor,
        Field = CompletionItemKind.Field,
        Variable = CompletionItemKind.Variable,
        Class = CompletionItemKind.Class,
        Interface = CompletionItemKind.Interface,
        Module = CompletionItemKind.Module,
        Property = CompletionItemKind.Property,
        Unit = CompletionItemKind.Unit,
        Value = CompletionItemKind.Value,
        Enum = CompletionItemKind.Enum,
        Keyword = CompletionItemKind.Keyword,
        Snippet = CompletionItemKind.Snippet,
        Color = CompletionItemKind.Color,
        File = CompletionItemKind.File,
        Reference = CompletionItemKind.Reference,
        Folder = CompletionItemKind.Folder,
        EnumMember = CompletionItemKind.EnumMember,
        Constant = CompletionItemKind.Constant,
        Struct = CompletionItemKind.Struct,
        Event = CompletionItemKind.Event,
        Operator = CompletionItemKind.Operator,
        TypeParameter = CompletionItemKind.TypeParameter,

        // Kinds custom to VS, starting with index 118115 to avoid collisions with other clients's custom kinds.

        Macro = LanguageServerConstants.VSCompletionItemKindOffset + 0,
        Namespace = LanguageServerConstants.VSCompletionItemKindOffset + 1,
        Template = LanguageServerConstants.VSCompletionItemKindOffset + 2,
        TypeDefinition = LanguageServerConstants.VSCompletionItemKindOffset + 3,
        Union = LanguageServerConstants.VSCompletionItemKindOffset + 4,
        Delegate = LanguageServerConstants.VSCompletionItemKindOffset + 5,
        TagHelper = LanguageServerConstants.VSCompletionItemKindOffset + 6,
        ExtensionMethod = LanguageServerConstants.VSCompletionItemKindOffset + 7,
        Element = LanguageServerConstants.VSCompletionItemKindOffset + 8,
        LocalResource = LanguageServerConstants.VSCompletionItemKindOffset + 9,
        SystemResource = LanguageServerConstants.VSCompletionItemKindOffset + 10,
    }
}
