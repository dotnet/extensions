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

            var formatter = new InplaceStringBuilder();
            formatter.Capacity += s1.Length + 1 + s2.Length;
            formatter.Append(s1);
            formatter.Append(c1);
            formatter.Append(s2);
            Assert.Equal("123456789", formatter.Build());
        }

        [Fact]
        public void Build_ThrowsIfNotEnoughWritten()
        {
            var formatter = new InplaceStringBuilder(5);
            formatter.Append("123");
            var exception = Assert.Throws<InvalidOperationException>(() => formatter.Build());
            Assert.Equal(exception.Message, "Entire reserved length was not used. Length: '5', written '3'.");
        }

        [Fact]
        public void AppendLength_IfAppendWasCalled()
        {
            var formatter = new InplaceStringBuilder(3);
            formatter.Append("123");

            var exception = Assert.Throws<InvalidOperationException>(() => formatter.Capacity = 5);
            Assert.Equal(exception.Message, "Cannot append length after write started.");
        }

        [Fact]
        public void Append_ThrowsIfNotEnoughSpace()
        {
            var formatter = new InplaceStringBuilder(1);

            var exception = Assert.Throws<InvalidOperationException>(() => formatter.Append("123"));
            Assert.Equal(exception.Message, "Not enough capacity to write '3' characters, only '1' left.");
        }

        [Fact]
        public void ToString_ReturnsPartialyFormatedValue()
        {
            var formatter = new InplaceStringBuilder(5);
            formatter.Append("123");

            Assert.Equal("123\0\0", formatter.ToString());
        }

        [Fact]
        public void ToString_ReturnedValueIsNotModified()
        {
            var formatter = new InplaceStringBuilder(5);
            formatter.Append("123");

            var s = formatter.ToString();
            Assert.Equal("123\0\0", s);

            formatter.Append("45");
            Assert.Equal("123\0\0", s);
        }
    }
}
