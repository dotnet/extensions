// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
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

            public string Name { get; }

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
        public void SnapshotDoesNotChangeUntilNextRequestOnConfigChanges()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            services.Configure<FakeOptions>(config);

            var sp = services.BuildServiceProvider();

            // Snapshot only updated once per scope
            using (var scope = sp.CreateScope())
            {
                var snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>();
                Assert.Equal("1", snapshot.Value.Message);
                config.Reload();
                Assert.Equal("1", snapshot.Value.Message);
            }

            using (var scope = sp.CreateScope())
            {
                var snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>();
                Assert.Equal("2", snapshot.Value.Message);
                config.Reload();
                Assert.Equal("2", snapshot.Value.Message);
            }
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
        public void SnapshotOptionsAreCachedPerScope()
        {
            var services = new ServiceCollection()
                .AddOptions()
                .AddScoped<IConfigureOptions<FakeOptions>, TestConfigure>()
                .BuildServiceProvider();

            var cache = services.GetRequiredService<IOptionsCache<FakeOptions>>();
            var factory = services.GetRequiredService<IServiceScopeFactory>();
            FakeOptions options = null;
            FakeOptions namedOne = null;
            using (var scope = factory.CreateScope())
            {
                options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value;
                Assert.Equal(options, scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value);
                namedOne = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Get("1");
                Assert.Equal(namedOne, scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Get("1"));
            }
            Assert.Equal(1, TestConfigure.ConfigureCount);
            Assert.True(cache.TryRemove(Options.DefaultName));
            Assert.True(cache.TryRemove("1"));
            using (var scope = factory.CreateScope())
            {
                var options2 = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value;
                Assert.NotEqual(options, options2);
                var namedOne2 = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Get("1");
                Assert.NotEqual(namedOne2, namedOne);
            }
            Assert.Equal(2, TestConfigure.ConfigureCount);
        }

        [Fact]
        public void CanResolveNamedOptionsSnapshot()
        {
            var services = new ServiceCollection().AddOptions();

            services.Configure<FakeOptions>("1", options =>
            {
                options.Message = "one";
            });
            services.Configure<FakeOptions>("2", options =>
            {
                options.Message = "two";
            });

            var sp = services.BuildServiceProvider();
            var option = sp.GetRequiredService<IOptionsSnapshot<FakeOptions>>();
            Assert.Equal("one", option.Get("1").Message);
            Assert.Equal("two", option.Get("2").Message);
        }

        [Fact]
        public void CanConfigureAllOptions()
        {
            var services = new ServiceCollection().AddOptions();
            services.ConfigureAll<FakeOptions>(o => o.Message = "Default");

            var sp = services.BuildServiceProvider();
            var option = sp.GetRequiredService<IOptionsSnapshot<FakeOptions>>();
            Assert.Equal("Default", option.Get("1").Message);
            Assert.Equal("Default", option.Get("2").Message);
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
            var option = sp.GetRequiredService<IOptionsSnapshot<FakeOptions>>();
            Assert.Equal("ABC", option.Get("1").Message);
            Assert.Equal("A+BC+", option.Get("+").Message);
            Assert.Equal("-ABC-", option.Get("-").Message);
        }

        [Fact]
        public void CanConfigureAllDefaultAndNamedOptions()
        {
            var services = new ServiceCollection().AddOptions();
            services.ConfigureAll<FakeOptions>(o => o.Message += "Default");
            services.Configure<FakeOptions>(o => o.Message += "0");
            services.Configure<FakeOptions>("1", o => o.Message += "1");

            var sp = services.BuildServiceProvider();
            var option = sp.GetRequiredService<IOptionsSnapshot<FakeOptions>>();
            Assert.Equal("Default", option.Get("Default").Message);
            Assert.Equal("Default0", option.Value.Message);
            Assert.Equal("Default1", option.Get("1").Message);
        }

        [Fact]
        public void EnsureAddOptionsLifetimes()
        {
            var services = new ServiceCollection().AddOptions();
            CheckLifetime(services, typeof(IOptions<>), ServiceLifetime.Singleton);
            CheckLifetime(services, typeof(IOptionsMonitor<>), ServiceLifetime.Singleton);
            CheckLifetime(services, typeof(IOptionsSnapshot<>), ServiceLifetime.Scoped);
            CheckLifetime(services, typeof(IOptionsCache<>), ServiceLifetime.Singleton);
            CheckLifetime(services, typeof(IOptionsFactory<>), ServiceLifetime.Transient);
        }

        private void CheckLifetime(IServiceCollection services, Type serviceType, ServiceLifetime lifetime)
        {
            Assert.NotNull(services.Where(s => s.ServiceType == serviceType && s.Lifetime == lifetime).SingleOrDefault());
        }
    }
}
