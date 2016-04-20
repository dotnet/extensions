// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders.Physical
{
    public class PollingFileChangeToken : IChangeToken
    {
        private readonly FileInfo _fileInfo;
        private DateTime _previousWriteTimeUtc;
        private DateTime _lastCheckedTimeUtc;
        private bool _hasChanged;

        public PollingFileChangeToken(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
            _previousWriteTimeUtc = GetLastWriteTimeUtc();
        }

        // Internal for unit testing
        internal static TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(4);

        private DateTime GetLastWriteTimeUtc()
        {
            _fileInfo.Refresh();
            return _fileInfo.Exists ? _fileInfo.LastWriteTimeUtc : DateTime.MinValue;
        }

        public bool ActiveChangeCallbacks => false;

        public bool HasChanged
        {
            get
            {
                if (_hasChanged)
                {
                    // Return true, if the change token ever changed in the past. The expecatation is that change tokens
                    // are not re-used once expired, so the caller must discard this instance once it sees it has changed.
                    return _hasChanged;
                }

                var currentTime = DateTime.UtcNow;
                if (currentTime - _lastCheckedTimeUtc < PollingInterval)
                {
                    return _hasChanged;
                }

                var lastWriteTimeUtc = GetLastWriteTimeUtc();
                if (_previousWriteTimeUtc != lastWriteTimeUtc)
                {
                    _previousWriteTimeUtc = lastWriteTimeUtc;
                    _hasChanged = true;
                }

                _lastCheckedTimeUtc = currentTime;
                return _hasChanged;
            }
        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state) => EmptyDisposable.Instance;
    }
}
