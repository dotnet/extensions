// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.FileSystemGlobbing.Internal.PathSegments
{
    public class ParentPathSegment : IPathSegment
    {
        private static readonly string LiteralParent = "..";

        public bool Match(string value, StringComparison comparisonType)
        {
            return string.Equals(LiteralParent, value, System.StringComparison.Ordinal);
        }
    }
}