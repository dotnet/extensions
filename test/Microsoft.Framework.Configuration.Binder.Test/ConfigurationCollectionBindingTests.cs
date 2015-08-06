// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Configuration.Memory;
using Xunit;

namespace Microsoft.Framework.Configuration.Binder.Test
{
    public class ConfigurationCollectionBinding
    {
        [Fact]
        public void StringListBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"StringList:0", "val0"},
                {"StringList:1", "val1"},
                {"StringList:2", "val2"},
                {"StringList:x", "valx"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(input));
            var config = builder.Build();

            var options = ConfigurationBinder.Bind<OptionsWithLists>(config);

            var list = options.StringList;

            Assert.Equal(4, list.Count);

            Assert.Equal("val0", list[0]);
            Assert.Equal("val1", list[1]);
            Assert.Equal("val2", list[2]);
            Assert.Equal("valx", list[3]);
        }

        [Fact]
        public void IntListBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"IntList:0", "42"},
                {"IntList:1", "43"},
                {"IntList:2", "44"},
                {"IntList:x", "45"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(input));
            var config = builder.Build();

            var options = ConfigurationBinder.Bind<OptionsWithLists>(config);

            var list = options.IntList;

            Assert.Equal(4, list.Count);

            Assert.Equal(42, list[0]);
            Assert.Equal(43, list[1]);
            Assert.Equal(44, list[2]);
            Assert.Equal(45, list[3]);
        }

        [Fact]
        public void AlreadyInitializedListBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"AlreadyInitializedList:0", "val0"},
                {"AlreadyInitializedList:1", "val1"},
                {"AlreadyInitializedList:2", "val2"},
                {"AlreadyInitializedList:x", "valx"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(input));
            var config = builder.Build();

            var options = ConfigurationBinder.Bind<OptionsWithLists>(config);

            var list = options.AlreadyInitializedList;

            Assert.Equal(5, list.Count);

            Assert.Equal("This was here before", list[0]);
            Assert.Equal("val0", list[1]);
            Assert.Equal("val1", list[2]);
            Assert.Equal("val2", list[3]);
            Assert.Equal("valx", list[4]);
        }

        [Fact]
        public void CustomListBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"CustomList:0", "val0"},
                {"CustomList:1", "val1"},
                {"CustomList:2", "val2"},
                {"CustomList:x", "valx"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(input));
            var config = builder.Build();

            var options = ConfigurationBinder.Bind<OptionsWithLists>(config);

            var list = options.CustomList;

            Assert.Equal(4, list.Count);

            Assert.Equal("val0", list[0]);
            Assert.Equal("val1", list[1]);
            Assert.Equal("val2", list[2]);
            Assert.Equal("valx", list[3]);
        }

        [Fact]
        public void ObjectListBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"ObjectList:0:Integer", "30"},
                {"ObjectList:1:Integer", "31"},
                {"ObjectList:2:Integer", "32"},
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(input));
            var config = builder.Build();

            var options = ConfigurationBinder.Bind<OptionsWithLists>(config);

            Assert.Equal(3, options.ObjectList.Count);

            Assert.Equal(30, options.ObjectList[0].Integer);
            Assert.Equal(31, options.ObjectList[1].Integer);
            Assert.Equal(32, options.ObjectList[2].Integer);
        }

        [Fact]
        public void NestedListsBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"NestedLists:0:0", "val00"},
                {"NestedLists:0:1", "val01"},
                {"NestedLists:1:0", "val10"},
                {"NestedLists:1:1", "val11"},
                {"NestedLists:1:2", "val12"},
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(input));
            var config = builder.Build();

            var options = ConfigurationBinder.Bind<OptionsWithLists>(config);

            Assert.Equal(2, options.NestedLists.Count);
            Assert.Equal(2, options.NestedLists[0].Count);
            Assert.Equal(3, options.NestedLists[1].Count);

            Assert.Equal("val00", options.NestedLists[0][0]);
            Assert.Equal("val01", options.NestedLists[0][1]);
            Assert.Equal("val10", options.NestedLists[1][0]);
            Assert.Equal("val11", options.NestedLists[1][1]);
            Assert.Equal("val12", options.NestedLists[1][2]);
        }

        [Fact]
        public void StringDictionaryBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"StringDictionary:abc", "val_1"},
                {"StringDictionary:def", "val_2"},
                {"StringDictionary:ghi", "val_3"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(input));
            var config = builder.Build();

            var options = ConfigurationBinder.Bind<OptionsWithDictionary>(config);

            Assert.Equal(3, options.StringDictionary.Count);

            Assert.Equal("val_1", options.StringDictionary["abc"]);
            Assert.Equal("val_2", options.StringDictionary["def"]);
            Assert.Equal("val_3", options.StringDictionary["ghi"]);
        }

        [Fact]
        public void IntDictionaryBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"IntDictionary:abc", "42"},
                {"IntDictionary:def", "43"},
                {"IntDictionary:ghi", "44"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(input));
            var config = builder.Build();

            var options = ConfigurationBinder.Bind<OptionsWithDictionary>(config);

            Assert.Equal(3, options.IntDictionary.Count);

            Assert.Equal(42, options.IntDictionary["abc"]);
            Assert.Equal(43, options.IntDictionary["def"]);
            Assert.Equal(44, options.IntDictionary["ghi"]);
        }

        [Fact]
        public void ObjectDictionary()
        {
            var input = new Dictionary<string, string>
            {
                {"ObjectDictionary:abc:Integer", "1"},
                {"ObjectDictionary:def:Integer", "2"},
                {"ObjectDictionary:ghi:Integer", "3"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(input));
            var config = builder.Build();

            var options = ConfigurationBinder.Bind<OptionsWithDictionary>(config);

            Assert.Equal(3, options.ObjectDictionary.Count);

            Assert.Equal(1, options.ObjectDictionary["abc"].Integer);
            Assert.Equal(2, options.ObjectDictionary["def"].Integer);
            Assert.Equal(3, options.ObjectDictionary["ghi"].Integer);
        }

        [Fact]
        public void ListDictionary()
        {
            var input = new Dictionary<string, string>
            {
                {"ListDictionary:abc:0", "abc_0"},
                {"ListDictionary:abc:1", "abc_1"},
                {"ListDictionary:def:0", "def_0"},
                {"ListDictionary:def:1", "def_1"},
                {"ListDictionary:def:2", "def_2"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(input));
            var config = builder.Build();

            var options = ConfigurationBinder.Bind<OptionsWithDictionary>(config);

            Assert.Equal(2, options.ListDictionary.Count);
            Assert.Equal(2, options.ListDictionary["abc"].Count);
            Assert.Equal(3, options.ListDictionary["def"].Count);

            Assert.Equal("abc_0", options.ListDictionary["abc"][0]);
            Assert.Equal("abc_1", options.ListDictionary["abc"][1]);
            Assert.Equal("def_0", options.ListDictionary["def"][0]);
            Assert.Equal("def_1", options.ListDictionary["def"][1]);
            Assert.Equal("def_2", options.ListDictionary["def"][2]);
        }

        [Fact]
        public void ListInNestedOptionBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"ObjectList:0:ListInNestedOption:0", "00"},
                {"ObjectList:0:ListInNestedOption:1", "01"},
                {"ObjectList:1:ListInNestedOption:0", "10"},
                {"ObjectList:1:ListInNestedOption:1", "11"},
                {"ObjectList:1:ListInNestedOption:2", "12"},
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(input));
            var config = builder.Build();

            var options = ConfigurationBinder.Bind<OptionsWithLists>(config);

            Assert.Equal(2, options.ObjectList.Count);
            Assert.Equal(2, options.ObjectList[0].ListInNestedOption.Count);
            Assert.Equal(3, options.ObjectList[1].ListInNestedOption.Count);

            Assert.Equal("00", options.ObjectList[0].ListInNestedOption[0]);
            Assert.Equal("01", options.ObjectList[0].ListInNestedOption[1]);
            Assert.Equal("10", options.ObjectList[1].ListInNestedOption[0]);
            Assert.Equal("11", options.ObjectList[1].ListInNestedOption[1]);
            Assert.Equal("12", options.ObjectList[1].ListInNestedOption[2]);
        }

        [Fact]
        public void NonStringKeyDictionaryBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"NonStringKeyDictionary:abc", "val_1"},
                {"NonStringKeyDictionary:def", "val_2"},
                {"NonStringKeyDictionary:ghi", "val_3"}
            };

            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(input));
            var config = builder.Build();

            var options = ConfigurationBinder.Bind<OptionsWithDictionary>(config);

            Assert.Equal(0, options.NonStringKeyDictionary.Count);
        }

        private class CustomList : List<string>
        {
            // Add an overload, just to make sure binding picks the right Add method
            public void Add(string a, string b)
            {
            }
        }

        private class CustomDictionary<T> : Dictionary<string, T>
        {
        }

        private class NestedOptions
        {
            public int Integer { get; set; }

            public List<string> ListInNestedOption { get; set; }
        }

        private class OptionsWithLists
        {
            public OptionsWithLists()
            {
                AlreadyInitializedList = new List<string>();
                AlreadyInitializedList.Add("This was here before");
            }

            public CustomList CustomList { get; set; }

            public List<string> StringList { get; set; }

            public List<int> IntList { get; set; }

            // This cannot be initialized because we cannot
            // activate an interface
            public IList<string> StringListInterface { get; set; }

            public List<List<string>> NestedLists { get; set; }

            public List<string> AlreadyInitializedList { get; set; }

            public List<NestedOptions> ObjectList { get; set; }
        }

        private class OptionsWithDictionary
        {
            public Dictionary<string, int> IntDictionary { get; set; }

            public Dictionary<string, string> StringDictionary { get; set; }

            public Dictionary<string, NestedOptions> ObjectDictionary { get; set; }

            public Dictionary<string, List<string>> ListDictionary { get; set; }

            public Dictionary<NestedOptions, string> NonStringKeyDictionary { get; set; }
        }
    }
}
