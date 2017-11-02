// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Internal
{
    public class TypeNameHelperTest
    {
        [Fact]
        public void Can_pretty_print_CLR_full_name()
        {
            // Predefined Types
            Assert.Equal("int",
                TypeNameHelper.GetTypeDisplayName(typeof(int)));
            Assert.Equal("System.Collections.Generic.List<int>",
                TypeNameHelper.GetTypeDisplayName(typeof(List<int>)));
            Assert.Equal("System.Collections.Generic.Dictionary<int, string>",
                TypeNameHelper.GetTypeDisplayName(typeof(Dictionary<int, string>)));
            Assert.Equal("System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>",
                TypeNameHelper.GetTypeDisplayName(typeof(Dictionary<int, List<string>>)));
            Assert.Equal("System.Collections.Generic.List<System.Collections.Generic.List<string>>",
                TypeNameHelper.GetTypeDisplayName(typeof(List<List<string>>)));

            // Classes inside NonGeneric class
            Assert.Equal("Microsoft.Extensions.Internal.TypeNameHelperTest+A",
                TypeNameHelper.GetTypeDisplayName(typeof(A)));
            Assert.Equal("Microsoft.Extensions.Internal.TypeNameHelperTest+B<int>",
                TypeNameHelper.GetTypeDisplayName(typeof(B<int>)));
            Assert.Equal("Microsoft.Extensions.Internal.TypeNameHelperTest+C<int, string>",
                TypeNameHelper.GetTypeDisplayName(typeof(C<int, string>)));
            Assert.Equal("Microsoft.Extensions.Internal.TypeNameHelperTest+C<int, Microsoft.Extensions.Internal.TypeNameHelperTest+B<string>>",
                TypeNameHelper.GetTypeDisplayName(typeof(C<int, B<string>>)));
            Assert.Equal("Microsoft.Extensions.Internal.TypeNameHelperTest+B<Microsoft.Extensions.Internal.TypeNameHelperTest+B<string>>",
                TypeNameHelper.GetTypeDisplayName(typeof(B<B<string>>)));

            // Classes inside Generic class
            Assert.Equal("Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+D",
                TypeNameHelper.GetTypeDisplayName(typeof(Outer<int>.D)));
            Assert.Equal("Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+E<int>",
                TypeNameHelper.GetTypeDisplayName(typeof(Outer<int>.E<int>)));
            Assert.Equal("Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+F<int, string>",
                TypeNameHelper.GetTypeDisplayName(typeof(Outer<int>.F<int, string>)));
            Assert.Equal("Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+F<int, Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+E<string>>",
                TypeNameHelper.GetTypeDisplayName(typeof(Outer<int>.F<int, Outer<int>.E<string>>)));
            Assert.Equal("Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+E<Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+E<string>>",
                TypeNameHelper.GetTypeDisplayName(typeof(Outer<int>.E<Outer<int>.E<string>>)));
            Assert.Equal("Microsoft.Extensions.Internal.TypeNameHelperTest+OuterGeneric<int>+InnerNonGeneric+InnerGeneric<int, string>+InnerGenericLeafNode<bool>",
                TypeNameHelper.GetTypeDisplayName(typeof(OuterGeneric<int>.InnerNonGeneric.InnerGeneric<int, string>.InnerGenericLeafNode<bool>)));
            Assert.Equal("Microsoft.Extensions.Internal.TypeNameHelperTest+Level1<int>+Level2<bool>+Level3<int>",
                TypeNameHelper.GetTypeDisplayName(typeof(Level1<int>.Level2<bool>.Level3<int>)));
        }

        [Fact]
        public void Can_pretty_print_CLR_name()
        {
            // Predefined Types
            Assert.Equal("int",
                TypeNameHelper.GetTypeDisplayName(typeof(int), false));
            Assert.Equal("List<int>",
                TypeNameHelper.GetTypeDisplayName(typeof(List<int>), false));
            Assert.Equal("Dictionary<int, string>",
                TypeNameHelper.GetTypeDisplayName(typeof(Dictionary<int, string>), false));
            Assert.Equal("Dictionary<int, List<string>>",
                TypeNameHelper.GetTypeDisplayName(typeof(Dictionary<int, List<string>>), false));
            Assert.Equal("List<List<string>>",
                TypeNameHelper.GetTypeDisplayName(typeof(List<List<string>>), false));

            // Classes inside NonGeneric class
            Assert.Equal("A",
                TypeNameHelper.GetTypeDisplayName(typeof(A), false));
            Assert.Equal("B<int>",
                TypeNameHelper.GetTypeDisplayName(typeof(B<int>), false));
            Assert.Equal("C<int, string>",
                TypeNameHelper.GetTypeDisplayName(typeof(C<int, string>), false));
            Assert.Equal("C<int, B<string>>",
                TypeNameHelper.GetTypeDisplayName(typeof(C<int, B<string>>), false));
            Assert.Equal("B<B<string>>",
                TypeNameHelper.GetTypeDisplayName(typeof(B<B<string>>), false));

            // Classes inside Generic class
            Assert.Equal("D",
                TypeNameHelper.GetTypeDisplayName(typeof(Outer<int>.D), false));
            Assert.Equal("E<int>",
                TypeNameHelper.GetTypeDisplayName(typeof(Outer<int>.E<int>), false));
            Assert.Equal("F<int, string>",
                TypeNameHelper.GetTypeDisplayName(typeof(Outer<int>.F<int, string>), false));
            Assert.Equal("F<int, E<string>>",
                TypeNameHelper.GetTypeDisplayName(typeof(Outer<int>.F<int, Outer<int>.E<string>>), false));
            Assert.Equal("E<E<string>>",
                TypeNameHelper.GetTypeDisplayName(typeof(Outer<int>.E<Outer<int>.E<string>>), false));
            Assert.Equal("InnerGenericLeafNode<bool>",
                TypeNameHelper.GetTypeDisplayName(typeof(OuterGeneric<int>.InnerNonGeneric.InnerGeneric<int, string>.InnerGenericLeafNode<bool>), false));
        }

        [Theory]
        [InlineData("void", typeof(void))]
        [InlineData("bool", typeof(bool))]
        [InlineData("byte", typeof(byte))]
        [InlineData("char", typeof(char))]
        [InlineData("decimal", typeof(decimal))]
        [InlineData("double", typeof(double))]
        [InlineData("float", typeof(float))]
        [InlineData("int", typeof(int))]
        [InlineData("long", typeof(long))]
        [InlineData("object", typeof(object))]
        [InlineData("sbyte", typeof(sbyte))]
        [InlineData("short", typeof(short))]
        [InlineData("string", typeof(string))]
        [InlineData("uint", typeof(uint))]
        [InlineData("ulong", typeof(ulong))]
        [InlineData("ushort", typeof(ushort))]
        public void Returns_common_name_for_built_in_types(string expected, Type type)
        {
            Assert.Equal(expected, TypeNameHelper.GetTypeDisplayName(type));
        }

        [Theory]
        [InlineData("int[]", typeof(int[]))]
        [InlineData("string[][]", typeof(string[][]))]
        [InlineData("int[,]", typeof(int[,]))]
        [InlineData("bool[,,,]", typeof(bool[,,,]))]
        [InlineData("Microsoft.Extensions.Internal.TypeNameHelperTest+A[,][,,]", typeof(A[,][,,]))]
        [InlineData("System.Collections.Generic.List<int[,][,,]>", typeof(List<int[,][,,]>))]
        [InlineData("List<int[,,][,]>[,][,,]", typeof(List<int[,,][,]>[,][,,]), false)]
        public void Can_pretty_print_array_name(string expected, Type type, bool fullName = true)
        {
            Assert.Equal(expected, TypeNameHelper.GetTypeDisplayName(type, fullName));
        }

        public static IEnumerable<object[]> GetOpenGenericsTestData()
        {
            yield return new object[] { "List<T>", typeof(List<>), false };
            yield return new object[] { "Dictionary<TKey, TValue>", typeof(Dictionary<,>), false };
            yield return new object[] { "System.Collections.Generic.List<T>", typeof(List<>) };
            yield return new object[] { "System.Collections.Generic.Dictionary<TKey, TValue>",typeof(Dictionary<,>) };

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+Level1<T1>+Level2<T2>+Level3<T3>",
                typeof(Level1<>.Level2<>.Level3<>)
            };

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+OuterGeneric<T1>+InnerNonGeneric+InnerGeneric<T2, T3>+InnerGenericLeafNode<T4>",
                typeof(OuterGeneric<>.InnerNonGeneric.InnerGeneric<,>.InnerGenericLeafNode<>)
            };

            var openDictionaryType = typeof(Dictionary<,>);
            var genArgsDictionary = openDictionaryType.GetGenericArguments();
            genArgsDictionary[0] = typeof(B<>);
            var closedDictionaryType = openDictionaryType.MakeGenericType(genArgsDictionary);

            yield return new object[]
            {
                "System.Collections.Generic.Dictionary<Microsoft.Extensions.Internal.TypeNameHelperTest+B<T>, TValue>",
                closedDictionaryType
            };

            var openLevelType = typeof(Level1<>.Level2<>.Level3<>);
            var genArgsLevel = openLevelType.GetGenericArguments();
            genArgsLevel[1] = typeof(string);
            var closedLevelType = openLevelType.MakeGenericType(genArgsLevel);

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+Level1<T1>+Level2<string>+Level3<T3>",
                closedLevelType
            };

            var openInnerType = typeof(OuterGeneric<>.InnerNonGeneric.InnerGeneric<,>.InnerGenericLeafNode<>);
            var genArgsInnerType = openInnerType.GetGenericArguments();
            genArgsInnerType[3] = typeof(bool);
            var closedInnerType = openInnerType.MakeGenericType(genArgsInnerType);

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+OuterGeneric<T1>+InnerNonGeneric+InnerGeneric<T2, T3>+InnerGenericLeafNode<bool>",
                closedInnerType
            };
        }

        [Theory]
        [MemberData(nameof(GetOpenGenericsTestData))]
        public void Can_pretty_print_open_generics(string expected, Type type, bool fullName = true)
        {
            Assert.Equal(expected, TypeNameHelper.GetTypeDisplayName(type, fullName));
        }

        private class A { }

        private class B<T> { }

        private class C<T1, T2> { }

        private class Outer<T>
        {
            public class D { }

            public class E<T1> { }

            public class F<T1, T2> { }
        }

        private class OuterGeneric<T1>
        {
            public class InnerNonGeneric
            {
                public class InnerGeneric<T2, T3>
                {
                    public class InnerGenericLeafNode<T4> { }

                    public class InnerLeafNode { }
                }
            }
        }

        private class Level1<T1>
        {
            public class Level2<T2>
            {
                public class Level3<T3>
                {
                }
            }
        }
    }
}
