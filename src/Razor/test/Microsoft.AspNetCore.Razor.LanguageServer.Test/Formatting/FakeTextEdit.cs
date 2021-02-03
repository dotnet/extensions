// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    internal class FakeTextEdit : ITextEdit
    {
        private readonly ITextSnapshot _initialSnapshot;
        private readonly EditOptions _options;
        private readonly int? _reiteratedVersionNumber;
        private readonly object _editTag;
        private readonly List<TextChange> _changes = new List<TextChange>();

        public FakeTextEdit(ITextSnapshot initialSnapshot, EditOptions options, int? reiteratedVersionNumber, object editTag)
        {
            _initialSnapshot = initialSnapshot;
            _options = options;
            _reiteratedVersionNumber = reiteratedVersionNumber;
            _editTag = editTag;
        }

        public bool HasEffectiveChanges => throw new NotImplementedException();

        public bool HasFailedChanges => throw new NotImplementedException();

        public ITextSnapshot Snapshot => throw new NotImplementedException();

        public bool Canceled => throw new NotImplementedException();

        public ITextSnapshot Apply()
        {
            if (_changes.Count == 0)
            {
                return _initialSnapshot;
            }

            throw new NotImplementedException();
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public bool Delete(Span deleteSpan)
        {
            throw new NotImplementedException();
        }

        public bool Delete(int startPosition, int charsToDelete)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public bool Insert(int position, string text)
        {
            throw new NotImplementedException();
        }

        public bool Insert(int position, char[] characterBuffer, int startIndex, int length)
        {
            throw new NotImplementedException();
        }

        public bool Replace(Span replaceSpan, string replaceWith)
        {
            throw new NotImplementedException();
        }

        public bool Replace(int startPosition, int charsToReplace, string replaceWith)
        {
            throw new NotImplementedException();
        }
    }
}
