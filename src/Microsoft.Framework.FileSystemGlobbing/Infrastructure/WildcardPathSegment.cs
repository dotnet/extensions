// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public class WildcardPathSegment : PatternSegment
    {
        public WildcardPathSegment(string beginsWith, List<string> contains, string endsWith)
        {
            BeginsWith = beginsWith;
            Contains = contains;
            EndsWith = endsWith;
        }

        public string BeginsWith { get; }

        public List<string> Contains { get; }

        public string EndsWith { get; }

        public override bool TestMatchingSegment(string value, StringComparison comparisonType)
        {
            var wildcard = this;

            if (value.Length < wildcard.BeginsWith.Length + wildcard.EndsWith.Length)
            {
                return false;
            }

            if (!value.StartsWith(wildcard.BeginsWith, comparisonType))
            {
                return false;
            }

            if (!value.EndsWith(wildcard.EndsWith, comparisonType))
            {
                return false;
            }

            var beginRemaining = wildcard.BeginsWith.Length;
            var endRemaining = value.Length - wildcard.EndsWith.Length;
            for (var containsIndex = 0; containsIndex != wildcard.Contains.Count; ++containsIndex)
            {
                var containsValue = wildcard.Contains[containsIndex];
                var indexOf = value.IndexOf(
                    value: containsValue,
                    startIndex: beginRemaining,
                    count: endRemaining - beginRemaining,
                    comparisonType: StringComparison.Ordinal);
                if (indexOf == -1)
                {
                    return false;
                }
                beginRemaining = indexOf + containsValue.Length;
            }

            return true;
        }
    }
}