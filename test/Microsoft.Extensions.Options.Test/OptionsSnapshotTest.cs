// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class OptionsSnapshotTest
    {
        public int SetupInvokeCount { get; set; }

        private class CountIncrement : IConfigureOptions<FakeOptions>
        {
            private OptionsSnapshotTest _test;

            public CountIncrement(OptionsSnapshotTest test)
            {
                _test = test;
            }

            public void Configure(FakeOptions options)
            {
                _test.SetupInvokeCount++;
                options.Message += _test.SetupInvokeCount;
            }
        }


        public class FakeSource : IOptionsChangeTokenSource<FakeOptions>
        {
            public FakeSource(FakeChangeToken token)
            {
                Token = token;
            }

            public FakeChangeToken Token { get; set; }

            public IChangeToken GetChangeToken()
            {
                return Token;
            }

            public void Changed()
            {
                Token.HasChanged = true;
                Token.InvokeChangeCallback();
            }
        }

        public class ControllerWithSnapshot
        {
            FakeOptions _options;

            public ControllerWithSnapshot(IOptionsSnapshot<FakeOptions> snap)
            {
                _options = snap.Value;
            }

            public string Message => _options?.Message;
        }

        [Fact]
        public void SnapshotOptionsDoNotChangeEvenWhenMonitorChanges()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            services.Configure<FakeOptions>(config);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            var snapshot = sp.GetRequiredService<IOptionsSnapshot<FakeOptions>>();

            var options = monitor.CurrentValue;
            Assert.Equal("1", options.Message);
            Assert.Equal("2", snapshot.Value.Message);
            Assert.NotEqual(options, snapshot.Value);

            var token = config.GetReloadToken();

            config.Reload();

            Assert.NotEqual(monitor.CurrentValue, snapshot.Value);
            Assert.Equal("3", monitor.CurrentValue.Message);
            Assert.Equal("2", snapshot.Value.Message);

            config.Reload();

            Assert.NotEqual(monitor.CurrentValue, snapshot.Value);
            Assert.Equal("4", monitor.CurrentValue.Message);
            Assert.Equal("2", snapshot.Value.Message);
        }

        private class TestConfigure : IConfigureOptions<FakeOptions>
        {
            public static int ConfigureCount;

            public TestConfigure()
            {
                ConfigureCount++;
            }

            public void Configure(FakeOptions options)
            {
            }
        }


        [Fact]
        public void SnapshotOptionsAreRecreatedPerScope()
        {
            var services = new ServiceCollection()
                .AddOptions()
                .AddScoped<IConfigureOptions<FakeOptions>, TestConfigure>()
                .BuildServiceProvider();

            var factory = services.GetRequiredService<IServiceScopeFactory>();
            FakeOptions options = null;
            using (var scope = factory.CreateScope())
            {
                options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value;
                Assert.Equal(options, scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value);
            }
            Assert.Equal(1, TestConfigure.ConfigureCount);
            using (var scope = factory.CreateScope())
            {
                var options2 = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value;
                Assert.Equal(options2, scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value);
                Assert.NotEqual(options, options2);
            }
            Assert.Equal(2, TestConfigure.ConfigureCount);
        }
    }
}
