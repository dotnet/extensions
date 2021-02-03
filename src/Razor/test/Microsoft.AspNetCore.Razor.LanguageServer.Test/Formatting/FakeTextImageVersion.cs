// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    internal class FakeTextImageVersion : ITextImageVersion
    {
        public FakeTextImageVersion(int length)
            : this(versionNumber: 0, reiteratedVersionNumber: 0, length, identifier: new object())
        {
        }

        public FakeTextImageVersion(int versionNumber, int reiteratedVersionNumber, int length, object identifier)
        {
            VersionNumber = versionNumber;
            ReiteratedVersionNumber = reiteratedVersionNumber;
            Identifier = identifier;
            Length = length;
        }

        public ITextImageVersion Next { get; private set; }

        public int Length { get; }

        public INormalizedTextChangeCollection Changes { get; private set; }

        public int VersionNumber { get; }

        public int ReiteratedVersionNumber { get; }

        public object Identifier { get; }

        public int TrackTo(VersionedPosition other, PointTrackingMode mode)
        {
            throw new NotImplementedException();
        }

        public Span TrackTo(VersionedSpan span, SpanTrackingMode mode)
        {
            throw new NotImplementedException();
        }

        internal FakeTextImageVersion CreateNext(int reiteratedVersionNumber, int length, INormalizedTextChangeCollection changes)
        {
            var versionNumber = VersionNumber + 1;
            if (reiteratedVersionNumber < 0)
            {
                reiteratedVersionNumber = changes is null || changes.Count == 0 ? ReiteratedVersionNumber : versionNumber;
            }

            if (Next is { })
                throw new InvalidOperationException();

            var newVersion = new FakeTextImageVersion(versionNumber, reiteratedVersionNumber, length, Identifier);
            Changes = changes;
            Next = newVersion;
            return newVersion;
        }
    }
}
