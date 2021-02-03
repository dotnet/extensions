// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    internal class FakeTextVersion : ITextVersion
    {
        public FakeTextVersion(ITextBuffer textBuffer, FakeTextImageVersion imageVersion)
        {
            TextBuffer = textBuffer;
            ImageVersion = imageVersion;
        }

        public ITextVersion Next { get; private set; }

        public int Length => ImageVersion.Length;

        public INormalizedTextChangeCollection Changes => ImageVersion.Changes;

        public ITextBuffer TextBuffer { get; }

        public int VersionNumber => ImageVersion.VersionNumber;

        public int ReiteratedVersionNumber => ImageVersion.ReiteratedVersionNumber;

        internal FakeTextImageVersion ImageVersion { get; }

        public ITrackingSpan CreateCustomTrackingSpan(Span span, TrackingFidelityMode trackingFidelity, object customState, CustomTrackToVersion behavior)
        {
            throw new NotImplementedException();
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode)
        {
            return CreateTrackingPoint(position, trackingMode, TrackingFidelityMode.Forward);
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode)
        {
            return CreateTrackingSpan(span.Start, span.Length, trackingMode, TrackingFidelityMode.Forward);
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            return CreateTrackingSpan(span.Start, span.Length, trackingMode, trackingFidelity);
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode)
        {
            return CreateTrackingSpan(start, length, trackingMode, TrackingFidelityMode.Forward);
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        internal FakeTextVersion CreateNext(INormalizedTextChangeCollection changes, int newLength = -1, int reiteratedVersionNumber = -1)
        {
            if (Next is { })
                throw new InvalidOperationException();

            var newImageVersion = ImageVersion.CreateNext(reiteratedVersionNumber, newLength, changes);
            var result = new FakeTextVersion(TextBuffer, newImageVersion);
            Next = result;

            return result;
        }
    }
}
