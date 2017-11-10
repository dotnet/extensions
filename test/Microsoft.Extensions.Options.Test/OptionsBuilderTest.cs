// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class OptionsBuilderTest
    {
        [Fact]
        public void CanSupportDefaultName()
        {
            var services = new ServiceCollection();
            var dic = new Dictionary<string, string>
            {
                { "Message", "!" },
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(dic).Build();

            var builder = services.AddOptions<FakeOptions>();
            builder
                .Configure(options => options.Message += "Igetstomped")
                .Bind(config)
                .PostConfigure(options => options.Message += "]")
                .Configure(options => options.Message += "[")
                .Configure(options => options.Message += "Default");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("![Default]", factory.Create(Options.DefaultName).Message);
        }

        [Fact]
        public void CanSupportNamedOptions()
        {
            var services = new ServiceCollection();
            var dic = new Dictionary<string, string>
            {
                { "Message", "!" },
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(dic).Build();

            var builder1 = services.AddOptions<FakeOptions>("1");
            var builder2 = services.AddOptions<FakeOptions>("2");
            builder1
                .Bind(config)
                .PostConfigure(options => options.Message += "]")
                .Configure(options => options.Message += "[")
                .Configure(options => options.Message += "one");
            builder2
                .Bind(config)
                .PostConfigure(options => options.Message += ">")
                .Configure(options => options.Message += "<")
                .Configure(options => options.Message += "two");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("![one]", factory.Create("1").Message);
            Assert.Equal("!<two>", factory.Create("2").Message);
        }

        [Fact]
        public void CanMixConfigureCallsOutsideBuilderInOrder()
        {
            var services = new ServiceCollection();
            var builder = services.AddOptions<FakeOptions>("1");
            
            services.ConfigureAll<FakeOptions>(o => o.Message += "A");
            builder.PostConfigure(o => o.Message += "D");
            services.PostConfigure<FakeOptions>("1", o => o.Message += "E");
            builder.Configure(o => o.Message += "B");
            services.Configure<FakeOptions>("1", o => o.Message += "C");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("ABCDE", factory.Create("1").Message);
        }
    }
}