// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Internal;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.PatternContexts
{
    internal class MockNonRecursivePathSegment : IPathSegment
    {
        public MockNonRecursivePathSegment(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public bool Match(string value, StringComparison comparisonType)
        {
            return string.Compare(Value, value, comparisonType) == 0;
        }
    }
}