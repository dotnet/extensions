// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class OptionsFactoryTest
    {
        [Fact]
        public void CreateSupportsNames()
        {
            var services = new ServiceCollection().AddOptions();
            services.Configure<FakeOptions>("1", options => options.Message = "one");
            services.Configure<FakeOptions>("2", options => options.Message = "two");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("one", factory.Create("1").Message);
            Assert.Equal("two", factory.Create("2").Message);
        }

        [Fact]
        public void CanConfigureAllOptions()
        {
            var services = new ServiceCollection().AddOptions();
            services.ConfigureAll<FakeOptions>(o => o.Message = "Default");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("Default", factory.Create("1").Message);
            Assert.Equal("Default", factory.Create(Options.DefaultName).Message);
            Assert.Equal("Default", factory.Create("2").Message);
        }

        [Fact]
        public void PostConfiguresInOrderAfterConfigures()
        {
            var services = new ServiceCollection().AddOptions();
            services.Configure<FakeOptions>("-", o => o.Message += "-");
            services.ConfigureAll<FakeOptions>(o => o.Message += "[");
            services.Configure<FakeOptions>("+", o => o.Message += "+");
            services.PostConfigure<FakeOptions>("-", o => o.Message += "-");
            services.PostConfigureAll<FakeOptions>(o => o.Message += "A");
            services.PostConfigure<FakeOptions>("+", o => o.Message += "+");
            services.PostConfigureAll<FakeOptions>(o => o.Message += "B");
            services.PostConfigureAll<FakeOptions>(o => o.Message += "C");
            services.PostConfigure<FakeOptions>("+", o => o.Message += "+");
            services.PostConfigure<FakeOptions>("-", o => o.Message += "-");
            services.Configure<FakeOptions>("+", o => o.Message += "+");
            services.ConfigureAll<FakeOptions>(o => o.Message += "]");
            services.Configure<FakeOptions>("-", o => o.Message += "-");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("[]ABC", factory.Create("1").Message);
            Assert.Equal("[++]A+BC+", factory.Create("+").Message);
            Assert.Equal("-[]--ABC-", factory.Create("-").Message);
        }

        [Fact]
        public void CanConfigureAndPostConfigureAllOptions()
        {
            var services = new ServiceCollection().AddOptions();
            services.ConfigureAll<FakeOptions>(o => o.Message = "D");
            services.PostConfigureAll<FakeOptions>(o => o.Message += "f");
            services.ConfigureAll<FakeOptions>(o => o.Message += "e");
            services.PostConfigureAll<FakeOptions>(o => o.Message += "ault");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("Default", factory.Create("1").Message);
            Assert.Equal("Default", factory.Create("2").Message);
        }

        [Fact]
        public void NamedSnapshotsConfiguresInRegistrationOrder()
        {
            var services = new ServiceCollection().AddOptions();
            services.Configure<FakeOptions>("-", o => o.Message += "-");
            services.ConfigureAll<FakeOptions>(o => o.Message += "A");
            services.Configure<FakeOptions>("+", o => o.Message += "+");
            services.ConfigureAll<FakeOptions>(o => o.Message += "B");
            services.ConfigureAll<FakeOptions>(o => o.Message += "C");
            services.Configure<FakeOptions>("+", o => o.Message += "+");
            services.Configure<FakeOptions>("-", o => o.Message += "-");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("ABC", factory.Create("1").Message);
            Assert.Equal("A+BC+", factory.Create("+").Message);
            Assert.Equal("-ABC-", factory.Create("-").Message);
        }

        [Fact]
        public void CanConfigureAllDefaultAndNamedOptions()
        {
            var services = new ServiceCollection().AddOptions();
            services.ConfigureAll<FakeOptions>(o => o.Message += "Default");
            services.Configure<FakeOptions>(o => o.Message += "0");
            services.Configure<FakeOptions>("1", o => o.Message += "1");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("Default", factory.Create("Default").Message);
            Assert.Equal("Default0", factory.Create(Options.DefaultName).Message);
            Assert.Equal("Default1", factory.Create("1").Message);
        }

        [Fact]
        public void CanConfigureAndPostConfigureAllDefaultAndNamedOptions()
        {
            var services = new ServiceCollection().AddOptions();
            services.ConfigureAll<FakeOptions>(o => o.Message += "Default");
            services.Configure<FakeOptions>(o => o.Message += "0");
            services.Configure<FakeOptions>("1", o => o.Message += "1");
            services.PostConfigureAll<FakeOptions>(o => o.Message += "PostConfigure");
            services.PostConfigure<FakeOptions>(o => o.Message += "2");
            services.PostConfigure<FakeOptions>("1", o => o.Message += "3");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("DefaultPostConfigure", factory.Create("Default").Message);
            Assert.Equal("Default0PostConfigure2", factory.Create(Options.DefaultName).Message);
            Assert.Equal("Default1PostConfigure3", factory.Create("1").Message);
        }

        [Fact]
        public void CanPostConfigureAllOptions()
        {
            var services = new ServiceCollection().AddOptions();
            services.PostConfigureAll<FakeOptions>(o => o.Message = "Default");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("Default", factory.Create("1").Message);
            Assert.Equal("Default", factory.Create("2").Message);
        }

        [Fact]
        public void CanPostConfigureAllDefaultAndNamedOptions()
        {
            var services = new ServiceCollection().AddOptions();
            services.PostConfigureAll<FakeOptions>(o => o.Message += "Default");
            services.PostConfigure<FakeOptions>(o => o.Message += "0");
            services.PostConfigure<FakeOptions>("1", o => o.Message += "1");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("Default", factory.Create("Default").Message);
            Assert.Equal("Default0", factory.Create(Options.DefaultName).Message);
            Assert.Equal("Default1", factory.Create("1").Message);
        }



    }
}