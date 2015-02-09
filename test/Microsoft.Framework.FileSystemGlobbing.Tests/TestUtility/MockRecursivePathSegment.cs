// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Internal;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.PatternContexts
{
    internal class MockRecursivePathSegment : IPathSegment
    {
        public MockRecursivePathSegment()
        {
        }

        public bool Match(string value, StringComparison comparisonType)
        {
            return false;
        }
    }
}