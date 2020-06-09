// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal abstract class RazorFormatOnTypeProvider
    {
        public abstract string TriggerCharacter { get; }

        public abstract bool TryFormatOnType(Position position, FormattingContext context, out TextEdit[] edits);
    }
}
