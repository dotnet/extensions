// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Framework.Caching;

namespace Microsoft.AspNet.FileProviders
{
    internal class FileChangeTrigger : IExpirationTrigger
    {
        private CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();

        public FileChangeTrigger(string pattern)
        {
            Pattern = pattern;
        }

        public string Pattern { get; private set; }

        private Regex _searchRegex;
        private Regex SearchRegex
        {
            get
            {
                if (_searchRegex == null)
                {
                    // Perf: Compile this as this may be used multiple times.
                    _searchRegex = new Regex('^' + Pattern + '$', RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                }

                return _searchRegex;
            }
        }

        public bool ActiveExpirationCallbacks
        {
            get { return true; }
        }

        public bool IsExpired
        {
            get { return TokenSource.Token.IsCancellationRequested; }
        }

        public IDisposable RegisterExpirationCallback(Action<object> callback, object state)
        {
            return TokenSource.Token.Register(callback, state);
        }

        public bool IsMatch(string relativePath)
        {
            return SearchRegex.IsMatch(relativePath);
        }

        public void Changed()
        {
            Task.Run(() =>
            {
                try
                {
                    TokenSource.Cancel();
                }
                catch
                {
                }
            });
        }
    }
}