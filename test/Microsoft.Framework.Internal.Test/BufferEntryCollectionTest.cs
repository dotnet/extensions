// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Framework.Internal
{
    public class BufferEntryCollectionTest
    {
        [Fact]
        public void Add_AddsBufferEntries()
        {
            // Arrange
            var collection = new BufferEntryCollection();
            var inner = new BufferEntryCollection();

            // Act
            collection.Add("Hello");
            collection.Add(new[] { 'a', 'b', 'c' }, 1, 2);
            collection.Add(inner);

            // Assert
            Assert.Equal(new[] { "Hello", "bc" }, collection);
            Assert.Same(inner.BufferEntries, collection.BufferEntries[2]);
        }

        [Fact]
        public void AddChar_ThrowsIfIndexIsOutOfBounds()
        {
            // Arrange
            var collection = new BufferEntryCollection();

            // Act and Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => collection.Add(new[] { 'a', 'b', 'c' }, -1, 2));
            Assert.Equal("index", ex.ParamName);
            Assert.Equal(
                "Specified argument was out of the range of valid values.\r\nParameter name: index",
                ex.Message);
        }

        [Fact]
        public void AddChar_ThrowsIfCountWouldCauseOutOfBoundReads()
        {
            // Arrange
            var collection = new BufferEntryCollection();

            // Act and Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => collection.Add(new[] { 'a', 'b', 'c' }, 1, 3));
            Assert.Equal("count", ex.ParamName);
            Assert.Equal(
                "Specified argument was out of the range of valid values.\r\nParameter name: count",
                ex.Message);
        }

        public static TheoryData AddWithChar_RepresentsStringsAsChunkedEntriesData
        {
            get
            {
                return new TheoryData<char[], int, int, IList<object>> {
                    {
                        new[] { 'a' }, 0, 1, new[] { "a" }
                    },
                    {
                        Enumerable.Repeat('a', 10).ToArray(),
                        0,
                        10,
                        new[] { new string(Enumerable.Repeat('a', 10).ToArray()) }
                    },
                    {
                        Enumerable.Repeat('b', 1024).ToArray(), 1, 1023, new[] { new string('b', 1023) }
                    },
                    {
                        Enumerable.Repeat('c', 1027).ToArray(), 1, 1026, new[] { new string('c', 1024), "cc" }
                    },
                    {
                        Enumerable.Repeat('d', 4099).ToArray(),
                        2,
                        4097,
                        new[]
                        {
                            new string('d', 1024),
                            new string('d', 1024),
                            new string('d', 1024),
                            new string('d', 1024),
                            "d"
                        }
                    },
                    {
                        Enumerable.Repeat('e', 1025).ToArray(), 1023, 2, new[] { "ee" }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(AddWithChar_RepresentsStringsAsChunkedEntriesData))]
        public void AddWithChar_RepresentsStringsAsChunkedEntries(char[] value, int index, int count, IList<object> expected)
        {
            // Arrange
            var collection = new BufferEntryCollection();

            // Act
            collection.Add(value, index, count);

            // Assert
            Assert.Equal(expected, collection.BufferEntries);
        }

        [Fact]
        public void Enumerator_TraversesThroughBuffer_SingleCollection()
        {
            // Arrange
            var collection = new BufferEntryCollection();
            collection.Add("foo");
            collection.Add("bar");

            var expected = new[]
                {
                    "foo",
                    "bar"
                };

            // Act and Assert
            Assert.Equal(expected, collection);
        }

        public static TheoryData NestedCollection_Data
        {
            get
            {
                return new TheoryData<bool, string[]>
                {
                    { false, new[] { "foo", "level 1", "level 2", "qux" } },
                    { true, new[] { "foo", "level 1", "qux" } }
                };
            }
        }

        [Theory]
        [MemberData(nameof(NestedCollection_Data))]
        public void Enumerator_TraversesThroughBuffer_NestedCollection(bool isEmpty, string[] expected)
        {
            // Arrange
            var nestedCollection = new BufferEntryCollection();
            nestedCollection.Add("level 1");
            var nestedCollection2SecondLevel = new BufferEntryCollection();
            if (!isEmpty)
            {
                nestedCollection2SecondLevel.Add("level 2");
            }

            nestedCollection.Add(nestedCollection2SecondLevel);
            var collection2 = new BufferEntryCollection();
            collection2.Add("foo");
            collection2.Add(nestedCollection);
            collection2.Add("qux");

            // Act & Assert
            Assert.Equal(expected, collection2);
        }
    }
}