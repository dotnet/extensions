using System;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests.Internal
{
    public class InplaceStringBuilderTest
    {
        [Fact]
        public void ToString_ReturnsStringWithAllAppendedValues()
        {
            var s1 = "123";
            var c1 = '4';
            var s2 = "56789";
            var seg = new StringSegment("890123", 2, 2);

            var formatter = new InplaceStringBuilder();
            formatter.Capacity += s1.Length + 1 + s2.Length + seg.Length;
            formatter.Append(s1);
            formatter.Append(c1);
            formatter.Append(s2, 0, 2);
            formatter.Append(s2, 2, 2);
            formatter.Append(s2, 4, 1);
            formatter.Append(seg);
            Assert.Equal("12345678901", formatter.ToString());
        }

        [Fact]
        public void Build_ThrowsIfNotEnoughWritten()
        {
            var formatter = new InplaceStringBuilder(5);
            formatter.Append("123");
            var exception = Assert.Throws<InvalidOperationException>(() => formatter.ToString());
            Assert.Equal("Entire reserved capacity was not used. Capacity: '5', written '3'.", exception.Message);
        }

        [Fact]
        public void Capacity_ThrowsIfAppendWasCalled()
        {
            var formatter = new InplaceStringBuilder(3);
            formatter.Append("123");

            var exception = Assert.Throws<InvalidOperationException>(() => formatter.Capacity = 5);
            Assert.Equal("Cannot change capacity after write started.", exception.Message);
        }

        [Fact]
        public void Append_ThrowsIfNotEnoughSpace()
        {
            var formatter = new InplaceStringBuilder(1);

            var exception = Assert.Throws<InvalidOperationException>(() => formatter.Append("123"));
            Assert.Equal("Not enough capacity to write '3' characters, only '1' left.", exception.Message);
        }
    }
}
