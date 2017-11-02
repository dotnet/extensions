// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Internal
{
    public class TypeNameHelperTest
    {
        public static IEnumerable<object[]> GetFullTypeNamesTestData()
        {
            // Predefined Types
            yield return new object[] { "int", typeof(int) };
            yield return new object[] { "System.Collections.Generic.List<int>", typeof(List<int>) };
            yield return new object[] { "System.Collections.Generic.Dictionary<int, string>", typeof(Dictionary<int, string>) };

            yield return new object[]
            {
                "System.Collections.Generic.List<System.Collections.Generic.List<string>>",
                typeof(List<List<string>>)
            };

            yield return new object[]
            {
                "System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>",
                typeof(Dictionary<int, List<string>>)
            };

            // Classes inside NonGeneric class
            yield return new object[] { "Microsoft.Extensions.Internal.TypeNameHelperTest+A", typeof(A) };
            yield return new object[] { "Microsoft.Extensions.Internal.TypeNameHelperTest+B<int>", typeof(B<int>) };
            yield return new object[] { "Microsoft.Extensions.Internal.TypeNameHelperTest+C<int, string>", typeof(C<int, string>) };

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+C<int, Microsoft.Extensions.Internal.TypeNameHelperTest+B<string>>",
                typeof(C<int, B<string>>)
            };

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+B<Microsoft.Extensions.Internal.TypeNameHelperTest+B<string>>",
                typeof(B<B<string>>)
            };

            // Classes inside Generic class
            yield return new object[] { "Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+D", typeof(Outer<int>.D) };
            yield return new object[] { "Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+E<int>", typeof(Outer<int>.E<int>) };
            yield return new object[] { "Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+F<int, string>", typeof(Outer<int>.F<int, string>) };

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+F<int, Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+E<string>>",
                typeof(Outer<int>.F<int, Outer<int>.E<string>>)
            };

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+E<Microsoft.Extensions.Internal.TypeNameHelperTest+Outer<int>+E<string>>",
                typeof(Outer<int>.E<Outer<int>.E<string>>)
            };

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+OuterGeneric<int>+InnerNonGeneric+InnerGeneric<int, string>+InnerGenericLeafNode<bool>",
                typeof(OuterGeneric<int>.InnerNonGeneric.InnerGeneric<int, string>.InnerGenericLeafNode<bool>)
            };

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+Level1<int>+Level2<bool>+Level3<int>",
                typeof(Level1<int>.Level2<bool>.Level3<int>)
            };
        }

        [Theory]
        [MemberData(nameof(GetFullTypeNamesTestData))]
        public void Can_pretty_print_CLR_full_name(string expected, Type type)
        {
            Assert.Equal(expected, TypeNameHelper.GetTypeDisplayName(type));
        }

        [Theory]
        // Predefined Types
        [InlineData("int", typeof(int))]
        [InlineData("List<int>", typeof(List<int>))]
        [InlineData("Dictionary<int, string>", typeof(Dictionary<int, string>))]
        [InlineData("Dictionary<int, List<string>>", typeof(Dictionary<int, List<string>>))]
        [InlineData("List<List<string>>", typeof(List<List<string>>))]
        // Classes inside NonGeneric class
        [InlineData("A", typeof(A))]
        [InlineData("B<int>", typeof(B<int>))]
        [InlineData("C<int, string>", typeof(C<int, string>))]
        [InlineData("C<int, B<string>>", typeof(C<int, B<string>>))]
        [InlineData("B<B<string>>", typeof(B<B<string>>))]
        // Classes inside Generic class
        [InlineData("D", typeof(Outer<int>.D))]
        [InlineData("E<int>", typeof(Outer<int>.E<int>))]
        [InlineData("F<int, string>", typeof(Outer<int>.F<int, string>))]
        [InlineData("F<int, E<string>>", typeof(Outer<int>.F<int, Outer<int>.E<string>>))]
        [InlineData("E<E<string>>", typeof(Outer<int>.E<Outer<int>.E<string>>))]
        [InlineData("InnerGenericLeafNode<bool>", typeof(OuterGeneric<int>.InnerNonGeneric.InnerGeneric<int, string>.InnerGenericLeafNode<bool>))]
        public void Can_pretty_print_CLR_name(string expected, Type type)
        {
            Assert.Equal(expected, TypeNameHelper.GetTypeDisplayName(type, false));
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
            yield return new object[] { "List<>", typeof(List<>), false };
            yield return new object[] { "Dictionary<,>", typeof(Dictionary<,>), false };
            yield return new object[] { "System.Collections.Generic.List<>", typeof(List<>) };
            yield return new object[] { "System.Collections.Generic.Dictionary<,>",typeof(Dictionary<,>) };

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+Level1<>+Level2<>+Level3<>",
                typeof(Level1<>.Level2<>.Level3<>)
            };

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+OuterGeneric<>+InnerNonGeneric+InnerGeneric<,>+InnerGenericLeafNode<>",
                typeof(OuterGeneric<>.InnerNonGeneric.InnerGeneric<,>.InnerGenericLeafNode<>)
            };

            var openDictionaryType = typeof(Dictionary<,>);
            var genArgsDictionary = openDictionaryType.GetGenericArguments();
            genArgsDictionary[0] = typeof(B<>);
            var closedDictionaryType = openDictionaryType.MakeGenericType(genArgsDictionary);

            yield return new object[]
            {
                "System.Collections.Generic.Dictionary<Microsoft.Extensions.Internal.TypeNameHelperTest+B<>,>",
                closedDictionaryType
            };

            var openLevelType = typeof(Level1<>.Level2<>.Level3<>);
            var genArgsLevel = openLevelType.GetGenericArguments();
            genArgsLevel[1] = typeof(string);
            var closedLevelType = openLevelType.MakeGenericType(genArgsLevel);

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+Level1<>+Level2<string>+Level3<>",
                closedLevelType
            };

            var openInnerType = typeof(OuterGeneric<>.InnerNonGeneric.InnerGeneric<,>.InnerGenericLeafNode<>);
            var genArgsInnerType = openInnerType.GetGenericArguments();
            genArgsInnerType[3] = typeof(bool);
            var closedInnerType = openInnerType.MakeGenericType(genArgsInnerType);

            yield return new object[]
            {
                "Microsoft.Extensions.Internal.TypeNameHelperTest+OuterGeneric<>+InnerNonGeneric+InnerGeneric<,>+InnerGenericLeafNode<bool>",
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
