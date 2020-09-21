// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    internal class VisualStudioTextChange : ITextChange
    {
        public VisualStudioTextChange(int oldStart, int oldLength, string newText)
        {
            OldSpan = new Span(oldStart, oldLength);
            NewText = newText;
        }

        public Span OldSpan { get; }
        public int OldPosition => this.OldSpan.Start;
        public int OldEnd => this.OldSpan.End;
        public int OldLength => this.OldSpan.Length;
        public string NewText { get; }
        public int NewLength => this.NewText.Length;

        public Span NewSpan => throw new NotImplementedException();

        public int NewPosition => throw new NotImplementedException();
        public int Delta => throw new NotImplementedException();
        public int NewEnd => throw new NotImplementedException();
        public string OldText => throw new NotImplementedException();
        public int LineCountDelta => throw new NotImplementedException();

        public override string ToString()
        {
            return this.OldSpan.ToString() + "->" + this.NewText;
        }
    }
}
