// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    internal abstract class LSPEditorService
    {
        public abstract Task ApplyTextEditsAsync(Uri uri, ITextSnapshot snapshot, IEnumerable<TextEdit> textEdits);

        public abstract void MoveCaretToPosition(string fullPath, int absoluteIndex);
    }
}
