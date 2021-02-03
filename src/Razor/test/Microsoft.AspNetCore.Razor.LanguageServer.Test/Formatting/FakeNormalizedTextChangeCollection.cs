// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    internal class FakeNormalizedTextChangeCollection : ReadOnlyCollection<ITextChange>, INormalizedTextChangeCollection
    {
        public static FakeNormalizedTextChangeCollection Empty { get; } = new FakeNormalizedTextChangeCollection(ImmutableArray<ITextChange>.Empty);

        public FakeNormalizedTextChangeCollection(ImmutableArray<ITextChange> list)
            : base(list)
        {
        }

        public bool IncludesLineChanges => throw new NotImplementedException();
    }
}
