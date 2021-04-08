// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert
{
    internal abstract class RazorOnAutoInsertProvider
    {
        public abstract string TriggerCharacter { get; }

        public abstract bool TryResolveInsertion(Position position, FormattingContext context, out TextEdit edit, out InsertTextFormat format);
    }
}
