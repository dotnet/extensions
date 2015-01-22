// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public abstract class PatternSegment
    {
        public virtual bool TestMatchingSegment(string value, StringComparison comparisonType)
        {
            return false;
        }
    }
}