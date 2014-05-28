// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;

namespace Microsoft.Framework.OptionsModel.Tests
{
    public class OptionsTest
    {
        public class ComplexOptions
        {
            public ComplexOptions()
            {
                Nested = new NestedOptions();
                Virtual = "complex";
            }
            public NestedOptions Nested { get; set; }
            public int Integer { get; set; }
            public bool Boolean { get; set; }
            public virtual string Virtual { get; set; }

            public string PrivateSetter { get; private set; }
            public string ProtectedSetter { get; protected set; }
            public string InternalSetter { get; internal set; }

            public string ReadOnly
            {
                get { return null; }
            }

        }

        public class NestedOptions
        {
            public int Integer { get; set; }
        }

        public class DerivedOptions : ComplexOptions
        {
            public override string Virtual
            {
                get
                {
                    return base.Virtual;
                }
                set
                {
                    base.Virtual = "Derived:" + value;
                }
            }
        }

        [Fact]
        public void CanReadComplexProperties()
        {
            var dic = new Dictionary<string, string>
            { 
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var config = new Configuration { new MemoryConfigurationSource(dic) };
            var options = new ComplexOptions();
            OptionsServices.ReadProperties(options, config);
            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
        }

        [Fact]
        public void CanReadInheritedProperties()
        {
            var dic = new Dictionary<string, string>
            { 
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"},
                {"Virtual","Sup"}
            };
            var config = new Configuration { new MemoryConfigurationSource(dic) };
            var options = new DerivedOptions();
            OptionsServices.ReadProperties(options, config);
            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
            Assert.Equal("Derived:Sup", options.Virtual);
        }

        [Theory]
        [InlineData("ReadOnly")]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        public void ShouldBeIgnoredTests(string property)
        {
            var dic = new Dictionary<string, string>
            { 
                {property, "stuff"},
            };
            var config = new Configuration { new MemoryConfigurationSource(dic) };
            var options = new ComplexOptions();
            OptionsServices.ReadProperties(options, config);
            Assert.Null(options.GetType().GetProperty(property).GetValue(options));
        }

        [Fact]
        public void SetupCallsSortedInOrder()
        {
            var services = new ServiceCollection { OptionsServices.GetDefaultServices() };
            var dic = new Dictionary<string, string>
            {
                {"Message", "!"},
            };
            var config = new Configuration { new MemoryConfigurationSource(dic) };
            services.SetupOptions<FakeOptions>(o => o.Message += "Igetstomped", -100000);
            services.SetupOptions<FakeOptions>(config);
            services.SetupOptions<FakeOptions>(o => o.Message += "a", -100);
            services.AddSetup<FakeOptionsSetupC>();
            services.AddSetup(new FakeOptionsSetupB());
            services.AddSetup(typeof(FakeOptionsSetupA));
            services.SetupOptions<FakeOptions>(o => o.Message += "z", 10000);

            var service = services.BuildServiceProvider().GetService<IOptionsAccessor<FakeOptions>>();
            Assert.NotNull(service);
            var options = service.Options;
            Assert.NotNull(options);
            Assert.Equal("!aABCz", options.Message);
        }
    }
}
