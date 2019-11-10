// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class EventIdTest
    {
        [Fact]
        public void Equality_operations()
        {
            Assert.True(new EventId(1).Equals(new EventId(1)));
            Assert.True(new EventId(1).Equals((object)new EventId(1)));
            Assert.True(new EventId(1).Equals(new EventId(1, "Foo")));
            Assert.True(new EventId(1, "Bar").Equals(new EventId(1, "Foo")));

            Assert.False(new EventId(1).Equals(new EventId(2)));
            Assert.False(new EventId(1).Equals(null));
            Assert.False(new EventId(1, "Foo").Equals(new EventId(2, "Foo")));

            Assert.True(new EventId(1) == new EventId(1));
            Assert.True(new EventId(1) == new EventId(1, "Foo"));
            Assert.True(new EventId(1, "Bar") == new EventId(1, "Foo"));

            Assert.True(new EventId(1) != new EventId(2));
            Assert.True(new EventId(1, "Foo") != new EventId(2, "Foo"));

            Assert.True(new EventId(1).GetHashCode() == new EventId(1).GetHashCode());
            Assert.True(new EventId(1).GetHashCode() == new EventId(1, "Foo").GetHashCode());
            Assert.True(new EventId(1, "Bar").GetHashCode() == new EventId(1, "Foo").GetHashCode());

            Assert.True(new EventId(1).GetHashCode() != new EventId(2).GetHashCode());
            Assert.True(new EventId(1, "Foo").GetHashCode() != new EventId(2, "Foo").GetHashCode());
        }

        [Fact]
        public void NameOnly_NameIsAssigned()
        {
            var foo = new EventId("Foo");

            var bar = new EventId("Bar");

            Assert.Equal("Foo", foo.Name);

            Assert.Equal("Bar", bar.Name);
        }
        
        [Fact]
        public void NameOnly_IdIsNonZero()
        {
            var foo = new EventId("Foo");

            var bar = new EventId("Bar");

            Assert.NotEqual(0, foo.Id);

            Assert.NotEqual(0, bar.Id);
        }

        [Fact]
        public void NameOnly_IdAreDistinct()
        {
            var foo = new EventId("Foo");

            var bar = new EventId("Bar");

            Assert.NotEqual(foo.Id, bar.Id);
            Assert.NotEqual(foo, bar);
        }

        [Fact]
        public void NameOnly_IdAreStable()
        {
            var foo1 = new EventId("Foo");

            var foo2 = new EventId("Foo");

            Assert.Equal(foo1.Id, foo2.Id);
            Assert.Equal(foo1, foo2);
        }
    }
}