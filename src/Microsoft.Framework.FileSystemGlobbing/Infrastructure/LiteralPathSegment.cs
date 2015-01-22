// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public class LiteralPathSegment : PatternSegment
    {
        public LiteralPathSegment(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override bool TestMatchingSegment(string value, StringComparison comparisonType)
        {
            if (!string.Equals(Value, value, comparisonType))
            {
                return false;
            }

            return true;
        }
    }
}