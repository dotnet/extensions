using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Framework.Internal
{
    public class HashCodeCombinerTest
    {
        [Fact]
        public void GivenTheSameInputs_ItProducesTheSameOutput()
        {
            var hashCode1 = HashCodeCombiner.Start().Add(42).Add("foo").CombinedHash;
            var hashCode2 = HashCodeCombiner.Start().Add(42).Add("foo").CombinedHash;
            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public void HashCode_Is_OrderSensitive()
        {
            var hashCode1 = HashCodeCombiner.Start().Add(42).Add("foo").CombinedHash;
            var hashCode2 = HashCodeCombiner.Start().Add("foo").Add(42).CombinedHash;
            Assert.NotEqual(hashCode1, hashCode2);
        }
    }
}
