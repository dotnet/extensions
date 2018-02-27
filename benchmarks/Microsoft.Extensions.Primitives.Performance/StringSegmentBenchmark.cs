using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Microsoft.Extensions.Primitives.Performance
{
    public class StringSegmentBenchmark
    {
        private readonly StringSegment _segment = new StringSegment("Hello world!");
        private readonly StringSegment _largeSegment = new StringSegment("Hello, World!, Hello people! My Car Is Cool. Your Carport is blue.");
        private readonly StringSegment _trimSegment = new StringSegment("   Hello world!    ");
        private readonly object _boxedSegment;
        private readonly char[] _indexOfAnyChars = { 'w', 'l' };

        public StringSegmentBenchmark()
        {
            _boxedSegment = _segment;
        }

        [Benchmark]
        public StringSegment Ctor_String()
        {
            return new StringSegment("Hello world!");
        }

        [Benchmark]
        public string GetValue() => _segment.Value;

        [Benchmark]
        public char Indexer() => _segment[3];

        [Benchmark]
        public bool Equals_Object_Invalid() => _segment.Equals(null as object);

        [Benchmark]
        public bool Equals_Object_Valid() => _segment.Equals(_boxedSegment);

        [Benchmark]
        public bool Equals_Valid() => _segment.Equals(_segment);

        [Benchmark]
        public bool Equals_String() => _segment.Equals("Hello world!");

        [Benchmark]
        public override int GetHashCode() => _segment.GetHashCode();

        [Benchmark]
        public bool StartsWith() => _largeSegment.StartsWith("Hel", StringComparison.Ordinal);

        [Benchmark]
        public bool EndsWith() => _largeSegment.EndsWith("ld!", StringComparison.Ordinal);

        [Benchmark]
        public string SubString() => _segment.Substring(3, 2);

        [Benchmark]
        public StringSegment SubSegment() => _segment.Subsegment(3, 2);

        [Benchmark]
        public int IndexOf() => _largeSegment.IndexOf(' ', 1, 7);

        [Benchmark]
        public int IndexOfAny() => _largeSegment.IndexOfAny(_indexOfAnyChars, 1, 7);

        [Benchmark]
        public int LastIndexOf() => _largeSegment.LastIndexOf('l');

        [Benchmark]
        public StringSegment Trim() => _trimSegment.Trim();

        [Benchmark]
        public StringSegment TrimStart() => _trimSegment.TrimStart();

        [Benchmark]
        public StringSegment TrimEnd() => _trimSegment.TrimEnd();
    }
}
