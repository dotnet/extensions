// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    internal class FakeTextImage : ITextImage
    {
        private readonly string _text;

        public FakeTextImage(string text, ITextImageVersion version)
        {
            _text = text;
            Version = version;
        }

        public char this[int position] => throw new NotImplementedException();

        public ITextImageVersion Version { get; }

        public int Length => _text.Length;

        public int LineCount => throw new NotImplementedException();

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            _text.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public TextImageLine GetLineFromLineNumber(int lineNumber)
        {
            throw new NotImplementedException();
        }

        public TextImageLine GetLineFromPosition(int position)
        {
            throw new NotImplementedException();
        }

        public int GetLineNumberFromPosition(int position)
        {
            throw new NotImplementedException();
        }

        public ITextImage GetSubText(Span span)
        {
            return new FakeTextImage(GetText(span), version: null);
        }

        public string GetText(Span span)
        {
            return _text.Substring(span.Start, span.Length);
        }

        public char[] ToCharArray(int startIndex, int length)
        {
            return _text.ToCharArray(startIndex, length);
        }

        public void Write(TextWriter writer, Span span)
        {
            throw new NotImplementedException();
        }
    }
}
