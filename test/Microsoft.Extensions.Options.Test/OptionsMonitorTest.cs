// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Options
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Used to fetch IChangeTokens.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public interface IOptionsChangeTokenSource<out TOptions>
    {
        /// <summary>
        /// Returns a IChangeToken which can be used to register a change notification callback.
        /// </summary>
        /// <returns></returns>
        IChangeToken GetChangeToken();
    }

    internal class ChangeTrackerDisposable : IDisposable
    {
        public List<IDisposable> Disposables { get; } = new List<IDisposable>();

        public void Dispose()
        {
            foreach (var d in Disposables)
            {
                d?.Dispose();
            }
            Disposables.Clear();
        }
    }

    /// <summary>
    /// Implementation of IOptionsMonitor.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public class OptionsMonitor<TOptions> : IOptionsMonitor<TOptions> where TOptions : class, new()
    {
        private OptionsCache<TOptions> _optionsCache;
        private readonly IEnumerable<IConfigureOptions<TOptions>> _setups;
        private readonly IEnumerable<IOptionsChangeTokenSource<TOptions>> _sources;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="setups">The configuration actions to run on an options instance.</param>
        /// <param name="sources">The sources used to listen for changes to the options instance.</param>
        public OptionsMonitor(IEnumerable<IConfigureOptions<TOptions>> setups, IEnumerable<IOptionsChangeTokenSource<TOptions>> sources)
        {
            _sources = sources;
            _setups = setups;
            _optionsCache = new OptionsCache<TOptions>(setups);
        }

        /// <inheritdoc></inheritdoc>
        public TOptions CurrentValue
        {
            get
            {
                return _optionsCache.Value;
            }
        }

        /// <inheritdoc></inheritdoc>
        public IDisposable OnChange(Action<TOptions> listener)
        {
            var disposable = new ChangeTrackerDisposable();
            foreach (var source in _sources)
            {

                Action<object> callback = null;
                IDisposable previousSubscription = null;
                callback = (s) =>
                {
                    // The order here is important. We need to take the token and then apply our changes BEFORE
                    // registering. This prevents us from possible having two change updates to process concurrently.
                    //
                    // If the token changes after we take the token, then we'll process the update immediately upon
                    // registering the callback.
                    var token = source.GetChangeToken();

                    // Recompute the options before calling the watchers
                    _optionsCache = new OptionsCache<TOptions>(_setups);
                    listener(_optionsCache.Value);

                    // Remove the old callback after its been fired
                    var nextSubscription = token.RegisterChangeCallback(callback, s);
                    disposable.Disposables.Add(nextSubscription);
                    disposable.Disposables.Remove(previousSubscription);
                    previousSubscription = nextSubscription;
                };

                previousSubscription = source.GetChangeToken().RegisterChangeCallback(callback, state: null);
                disposable.Disposables.Add(previousSubscription);
            }
            return disposable;
        }
    }

    /// <summary>
    /// Creates IChangeTokens so that IOptionsMonitor gets notified when IConfiguration changes.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public class ConfigurationChangeTokenSource<TOptions> : IOptionsChangeTokenSource<TOptions>
    {
        private IConfiguration _config;

        /// <summary>
        /// Constructor taking the IConfiguration instance to watch.
        /// </summary>
        /// <param name="config">The configuration instance.</param>
        public ConfigurationChangeTokenSource(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            _config = config;
        }

        /// <summary>
        /// Returns the reloadToken from IConfiguration.
        /// </summary>
        /// <returns></returns>
        public IChangeToken GetChangeToken()
        {
            return _config.GetReloadToken();
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding configuration related options services to the DI container.
    /// </summary>
    public static class OptionsMonitorServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a configuration instance which TOptions will bind against.
        /// </summary>
        /// <typeparam name="TOptions">The type of options being configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="config">The configuration being bound.</param>
        /// <param name="trackConfigChanges">If true, registers an change token source for use by an IOptionsMonitor.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection Configure<TOptions>(this IServiceCollection services, IConfiguration config, bool trackConfigChanges)
            where TOptions : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.AddSingleton<IConfigureOptions<TOptions>>(new ConfigureFromConfigurationOptions<TOptions>(config));
            if (trackConfigChanges)
            {
                services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(new ConfigurationChangeTokenSource<TOptions>(config));
            }
            return services;
        }

        public static IServiceCollection AddOptionsMonitor(this IServiceCollection services)
        {
            services.AddOptions().TryAdd(ServiceDescriptor.Singleton(typeof(IOptionsMonitor<>), typeof(OptionsMonitor<>)));
            return services;
        }
    }
}
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
            var services = new ServiceCollection().AddOptionsMonitor();

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
        public void CanWatchOptionsWithMultipleSources()
        {
            var services = new ServiceCollection().AddOptionsMonitor();
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
            Assert.Equal("3", monitor.CurrentValue.Message);
        }

        [Fact]
        public void CanMonitorConfigBoundOptions()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var services = new ServiceCollection().AddOptionsMonitor();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            services.Configure<FakeOptions>(config, trackConfigChanges: true);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", monitor.CurrentValue.Message);

            string updatedMessage = null;

            var cleanup = monitor.OnChange(o => updatedMessage = o.Message) as ChangeTrackerDisposable;

            config.Reload();
            Assert.Equal("2", updatedMessage);

            // Verify old watch is changed too
            Assert.Equal("2", monitor.CurrentValue.Message);

            Assert.Equal(1, cleanup.Disposables.Count);

            cleanup.Dispose();
            config.Reload();

            // Verify things don't change after the subscription is disposed
            Assert.Equal("2", updatedMessage);
            Assert.Equal("2", monitor.CurrentValue.Message);
        }

        public class Controller : IDisposable
        {
            IDisposable _watcher;
            FakeOptions _options;

            public Controller(IOptionsMonitor<FakeOptions> watcher)
            {
                _watcher = watcher.OnChange(o => _options = o);
            }

            public void Dispose()
            {
                _watcher?.Dispose();
            }

            public string Message => _options?.Message;
        }

        [Fact]
        public void ControllerCanWatchOptionsThatTrackConfigChanges()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var services = new ServiceCollection().AddOptionsMonitor();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            services.AddTransient<Controller, Controller>();
            services.Configure<FakeOptions>(config, trackConfigChanges: true);

            var sp = services.BuildServiceProvider();

            var controller = sp.GetRequiredService<Controller>();
            Assert.Null(controller.Message);

            config.Reload();
            Assert.Equal("1", controller.Message);

            config.Reload();
            Assert.Equal("2", controller.Message);
        }
    }
}