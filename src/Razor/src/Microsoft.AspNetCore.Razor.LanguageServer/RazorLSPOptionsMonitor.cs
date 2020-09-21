// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLSPOptionsMonitor : IOptionsMonitor<RazorLSPOptions>
    {
        private readonly RazorConfigurationService _configurationService;
        private readonly IOptionsMonitorCache<RazorLSPOptions> _cache;
        internal event Action<RazorLSPOptions, string> _onChange;
        private RazorLSPOptions _currentValue;

        public RazorLSPOptionsMonitor(RazorConfigurationService configurationService, IOptionsMonitorCache<RazorLSPOptions> cache)
        {
            if (configurationService is null)
            {
                throw new ArgumentNullException(nameof(configurationService));
            }

            if (cache is null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            _configurationService = configurationService;
            _cache = cache;
            _currentValue = RazorLSPOptions.Default;
        }

        public RazorLSPOptions CurrentValue => Get(Options.DefaultName);

        public RazorLSPOptions Get(string name)
        {
            name = name ?? Options.DefaultName;
            return _cache.GetOrAdd(name, () => _currentValue);
        }

        public IDisposable OnChange(Action<RazorLSPOptions, string> listener)
        {
            var disposable = new ChangeTrackerDisposable(this, listener);
            _onChange += disposable.OnChange;
            return disposable;
        }

        public virtual async Task UpdateAsync(CancellationToken cancellationToken)
        {
            var latestOptions = await _configurationService.GetLatestOptionsAsync(cancellationToken);
            if (latestOptions != null)
            {
                _currentValue = latestOptions;
                InvokeChanged();
            }
        }

        private void InvokeChanged()
        {
            var name = Options.DefaultName;
            _cache.TryRemove(name);
            var options = Get(name);
            _onChange?.Invoke(options, name);
        }

        internal class ChangeTrackerDisposable : IDisposable
        {
            private readonly Action<RazorLSPOptions, string> _listener;
            private readonly RazorLSPOptionsMonitor _monitor;

            public ChangeTrackerDisposable(RazorLSPOptionsMonitor monitor, Action<RazorLSPOptions, string> listener)
            {
                _listener = listener;
                _monitor = monitor;
            }

            public void OnChange(RazorLSPOptions options, string name) => _listener.Invoke(options, name);

            public void Dispose() => _monitor._onChange -= OnChange;
        }
    }
}
