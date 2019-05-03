// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Text
{
    internal static class SourceTextExtensions
    {
        public static RazorSourceDocument GetRazorSourceDocument(this SourceText sourceText, string filePath, string relativePath)
        {
            if (sourceText == null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            var sourceDocument = new SourceTextSourceDocument(sourceText, filePath, relativePath);
            return sourceDocument;
        }

        // Internal for testing
        internal class SourceTextSourceDocument : RazorSourceDocument
        {
            private readonly SourceText _sourceText;
            private byte[] _checksum;

            public SourceTextSourceDocument(
                SourceText sourceText,
                string filePath,
                string relativeFilePath)
            {
                _sourceText = sourceText;

                FilePath = filePath;
                RelativePath = relativeFilePath;
                Encoding = Encoding.UTF8;
                Lines = new RazorTextLineCollection(_sourceText.Lines);
            }

            public override char this[int position] => _sourceText[position];

            public override Encoding Encoding { get; }

            public override string FilePath { get; }

            public override string RelativePath { get; }

            public override int Length => _sourceText.Length;

            public override RazorSourceLineCollection Lines { get; }

            public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            {
                _sourceText.CopyTo(sourceIndex, destination, destinationIndex, count);
            }

            public override byte[] GetChecksum()
            {
                if (_checksum == null)
                {
                    _checksum = _sourceText.GetChecksum().ToArray();
                }

                return _checksum;
            }
        }

        private class RazorTextLineCollection : RazorSourceLineCollection
        {
            private readonly TextLineCollection _textLineCollection;

            public RazorTextLineCollection(TextLineCollection textLineCollection)
            {
                _textLineCollection = textLineCollection;
            }

            public override int Count => _textLineCollection.Count;

            public override int GetLineLength(int index)
            {
                var textLineLength = _textLineCollection[index].SpanIncludingLineBreak.Length;
                return textLineLength;
            }

            internal override SourceLocation GetLocation(int position)
            {
                var textLine = _textLineCollection.GetLineFromPosition(position);
                var sourceLocation = new SourceLocation(position, textLine.LineNumber, position - textLine.Start);

                return sourceLocation;
            }
        }
    }
}
