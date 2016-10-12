// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class OptionsMonitorTest
    {
        public int SetupInvokeCount { get; set; }

        public class FakeChangeToken : IChangeToken, IDisposable
        {
            public bool ActiveChangeCallbacks { get; set; }
            public bool HasChanged { get; set; }
            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                _callback = () => callback(state);
                return this;
            }

            public void InvokeChangeCallback()
            {
                if (_callback != null)
                {
                    _callback();
                }
            }

            public void Dispose()
            {
                _callback = null;
            }

            private Action _callback;
        }

        public class CountIncrement : IConfigureOptions<FakeOptions>
        {
            private OptionsMonitorTest _test;

            public CountIncrement(OptionsMonitorTest test)
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

        [Fact]
        public void CanWatchOptions()
        {
            var services = new ServiceCollection().AddOptions();

            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            var tracker = new FakeSource(changeToken);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(tracker);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", monitor.CurrentValue.Message);

            string updatedMessage = null;
            monitor.OnChange(o => updatedMessage = o.Message);
            changeToken.InvokeChangeCallback();
            Assert.Equal("2", updatedMessage);

            // Verify old watch is changed too
            Assert.Equal("2", monitor.CurrentValue.Message);
        }

        [Fact]
        public void CanWatchOptionsWithMultipleSourcesAndCallbacks()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            var tracker = new FakeSource(changeToken);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(tracker);
            var changeToken2 = new FakeChangeToken();
            var tracker2 = new FakeSource(changeToken2);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(tracker2);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", monitor.CurrentValue.Message);

            string updatedMessage = null;
            string updatedMessage2 = null;
            var cleanup = monitor.OnChange(o => updatedMessage = o.Message);
            var cleanup2 = monitor.OnChange(o => updatedMessage2 = o.Message);
            changeToken.InvokeChangeCallback();
            Assert.Equal("2", updatedMessage);
            Assert.Equal("2", updatedMessage2);

            // Verify old watch is changed too
            Assert.Equal("2", monitor.CurrentValue.Message);

            changeToken2.InvokeChangeCallback();
            Assert.Equal("3", updatedMessage);
            Assert.Equal("3", updatedMessage2);

            // Verify old watch is changed too
            Assert.Equal("3", monitor.CurrentValue.Message);

            cleanup.Dispose();
            changeToken.InvokeChangeCallback();
            changeToken2.InvokeChangeCallback();

            // Verify only the second message changed
            Assert.Equal("3", updatedMessage);
            Assert.Equal("5", updatedMessage2);

            cleanup2.Dispose();
            changeToken.InvokeChangeCallback();
            changeToken2.InvokeChangeCallback();

            // Verify no message changed
            Assert.Equal("3", updatedMessage);
            Assert.Equal("5", updatedMessage2);
        }

        [Fact]
        public void CanWatchOptionsWithMultipleSources()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            var tracker = new FakeSource(changeToken);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(tracker);
            var changeToken2 = new FakeChangeToken();
            var tracker2 = new FakeSource(changeToken2);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(tracker2);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", monitor.CurrentValue.Message);

            string updatedMessage = null;
            var cleanup = monitor.OnChange(o => updatedMessage = o.Message);
            changeToken.InvokeChangeCallback();
            Assert.Equal("2", updatedMessage);

            // Verify old watch is changed too
            Assert.Equal("2", monitor.CurrentValue.Message);

            changeToken2.InvokeChangeCallback();
            Assert.Equal("3", updatedMessage);

            // Verify old watch is changed too
            Assert.Equal("3", monitor.CurrentValue.Message);

            cleanup.Dispose();
            changeToken.InvokeChangeCallback();
            changeToken2.InvokeChangeCallback();

            // Verify messages aren't changed
            Assert.Equal("3", updatedMessage);
        }

        [Fact]
        public void CanMonitorConfigBoundOptions()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            services.Configure<FakeOptions>(config);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", monitor.CurrentValue.Message);

            string updatedMessage = null;

            var cleanup = monitor.OnChange(o => updatedMessage = o.Message);

            config.Reload();
            Assert.Equal("2", updatedMessage);

            // Verify old watch is changed too
            Assert.Equal("2", monitor.CurrentValue.Message);

            cleanup.Dispose();
            config.Reload();

            // Verify our message don't change after the subscription is disposed
            Assert.Equal("2", updatedMessage);

            // But the monitor still gets updated with the latest current value
            Assert.Equal("3", monitor.CurrentValue.Message);
        }

        public class ControllerWithMonitor : IDisposable
        {
            IDisposable _watcher;
            FakeOptions _options;

            public ControllerWithMonitor(IOptionsMonitor<FakeOptions> watcher)
            {
                _watcher = watcher.OnChange(o => _options = o);
            }

            public void Dispose()
            {
                _watcher?.Dispose();
            }

            public string Message => _options?.Message;
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
        public void ControllerCanWatchOptionsThatTrackConfigChanges()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            services.AddTransient<ControllerWithMonitor, ControllerWithMonitor>();
            services.Configure<FakeOptions>(config);

            var sp = services.BuildServiceProvider();

            var controller = sp.GetRequiredService<ControllerWithMonitor>();
            Assert.Null(controller.Message);

            config.Reload();
            Assert.Equal("1", controller.Message);

            config.Reload();
            Assert.Equal("2", controller.Message);
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
            Assert.Equal(options, snapshot.Value);

            var token = config.GetReloadToken();

            config.Reload();

            Assert.NotEqual(monitor.CurrentValue, snapshot.Value);
            Assert.Equal("2", monitor.CurrentValue.Message);
            Assert.Equal("1", snapshot.Value.Message);

            config.Reload();

            Assert.NotEqual(monitor.CurrentValue, snapshot.Value);
            Assert.Equal("3", monitor.CurrentValue.Message);
            Assert.Equal("1", snapshot.Value.Message);
        }
    }
}