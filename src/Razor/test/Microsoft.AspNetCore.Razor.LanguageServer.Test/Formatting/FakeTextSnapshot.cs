// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    internal class FakeTextSnapshot : ITextSnapshot2
    {
        public FakeTextSnapshot(ITextBuffer textBuffer, ITextVersion version, ITextImage textImage)
        {
            TextBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
            ContentType = textBuffer.ContentType;
            Version = version ?? throw new ArgumentNullException(nameof(version));

            TextImage = textImage ?? throw new ArgumentNullException(nameof(textImage));
        }

        public ITextBuffer TextBuffer { get; }

        public IContentType ContentType { get; }

        public ITextVersion Version { get; }

        public int Length => TextImage.Length;

        public int LineCount => TextImage.LineCount;

        public IEnumerable<ITextSnapshotLine> Lines => throw new NotImplementedException();

        public ITextImage TextImage { get; }

        public char this[int position] => TextImage[position];

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            TextImage.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode)
        {
            throw new NotImplementedException();
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITextSnapshotLine GetLineFromLineNumber(int lineNumber)
        {
            throw new NotImplementedException();
        }

        public ITextSnapshotLine GetLineFromPosition(int position)
        {
            throw new NotImplementedException();
        }

        public int GetLineNumberFromPosition(int position)
        {
            return TextImage.GetLineNumberFromPosition(position);
        }

        public string GetText()
        {
            return TextImage.GetText();
        }

        public string GetText(Span span)
        {
            return TextImage.GetText(span);
        }

        public string GetText(int startIndex, int length)
        {
            return TextImage.GetText(startIndex, length);
        }

        public char[] ToCharArray(int startIndex, int length)
        {
            return TextImage.ToCharArray(startIndex, length);
        }

        public void Write(TextWriter writer, Span span)
        {
            TextImage.Write(writer, span);
        }

        public void Write(TextWriter writer)
        {
            TextImage.Write(writer);
        }

        public void SaveToFile(string filePath, bool replaceFile, Encoding encoding)
        {
            throw new NotImplementedException();
        }
    }
}
