// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public class Pattern
    {
        private static char[] _slashes = new[] { '/', '\\' };
        private static char[] _star = new[] { '*' };

        public Pattern(string pattern)
        {
            var endPattern = pattern.Length;
            for (int scanPattern = 0; scanPattern < endPattern;)
            {
                var beginSegment = scanPattern;
                var endSegment = NextIndex(pattern, _slashes, scanPattern, endPattern);

                PatternSegment segment = null;

                if (segment == null && endSegment - beginSegment == 3)
                {
                    if (pattern[beginSegment] == '*' &&
                        pattern[beginSegment + 1] == '.' &&
                        pattern[beginSegment + 2] == '*')
                    {
                        // turn *.* into *
                        beginSegment += 2;
                    }
                }

                if (segment == null && endSegment - beginSegment == 2)
                {
                    if (pattern[beginSegment] == '*' &&
                        pattern[beginSegment + 1] == '*')
                    {
                        // recognized **
                        segment = new RecursiveWildcardSegment();
                    }
                    else if (pattern[beginSegment] == '.' &&
                        pattern[beginSegment + 1] == '.')
                    {
                        // recognized ..
                        segment = new ParentPathSegment();
                    }
                }

                if (segment == null && endSegment - beginSegment == 1)
                {
                    if (pattern[beginSegment] == '.')
                    {
                        // recognized .
                        segment = new CurrentPathSegment();
                    }
                }

                if (segment == null)
                {
                    var beginsWith = string.Empty;
                    var contains = new List<string>();
                    var endsWith = string.Empty;

                    for (int scanSegment = beginSegment; scanSegment < endSegment;)
                    {
                        var beginLiteral = scanSegment;
                        var endLiteral = NextIndex(pattern, _star, scanSegment, endSegment);

                        if (beginLiteral == beginSegment)
                        {
                            if (endLiteral == endSegment)
                            {
                                // and the only bit
                                segment = new LiteralPathSegment(Portion(pattern, beginLiteral, endLiteral));
                            }
                            else
                            {
                                // this is the first bit
                                beginsWith = Portion(pattern, beginLiteral, endLiteral);
                            }
                        }
                        else if (endLiteral == endSegment)
                        {
                            // this is the last bit
                            endsWith = Portion(pattern, beginLiteral, endLiteral);
                        }
                        else
                        {
                            if (beginLiteral != endLiteral)
                            {
                                // this is a middle bit
                                contains.Add(Portion(pattern, beginLiteral, endLiteral));
                            }
                            else
                            {
                                // note: NOOP here, adjacent *'s are collapsed when they
                                // are mixed with literal text in a path segment
                            }
                        }

                        scanSegment = endLiteral + 1;
                    }

                    if (segment == null)
                    {
                        segment = new WildcardPathSegment(beginsWith, contains, endsWith);
                    }
                }

                if (segment is RecursiveWildcardSegment)
                {
                    if (StartsWith == null)
                    {
                        StartsWith = new List<PatternSegment>(Segments);
                        EndsWith = new List<PatternSegment>();
                        Contains = new List<IList<PatternSegment>>();
                    }
                    else if (EndsWith.Count != 0)
                    {
                        Contains.Add(EndsWith);
                        EndsWith = new List<PatternSegment>();
                    }
                }
                else if (EndsWith != null)
                {
                    EndsWith.Add(segment);
                }

                Segments.Add(segment);

                scanPattern = endSegment + 1;
            }
        }

        public IList<PatternSegment> Segments { get; } = new List<PatternSegment>();

        public IList<PatternSegment> StartsWith { get; }

        public IList<IList<PatternSegment>> Contains { get; }

        public IList<PatternSegment> EndsWith { get; }

        private int NextIndex(string pattern, char[] anyOf, int beginIndex, int endIndex)
        {
            var index = pattern.IndexOfAny(anyOf, beginIndex, endIndex - beginIndex);
            return index == -1 ? endIndex : index;
        }

        private string Portion(string pattern, int beginIndex, int endIndex)
        {
            return pattern.Substring(beginIndex, endIndex - beginIndex);
        }
    }
}