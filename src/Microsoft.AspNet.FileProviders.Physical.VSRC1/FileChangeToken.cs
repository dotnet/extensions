// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives.VSRC1;

namespace Microsoft.AspNet.FileProviders.VSRC1
{
    internal class FileChangeToken : IChangeToken
    {
        private Regex _searchRegex;

        public FileChangeToken(string pattern)
        {
            Pattern = pattern;
        }

        public string Pattern { get; }

        private CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();

        private Regex SearchRegex
        {
            get
            {
                if (_searchRegex == null)
                {
                    _searchRegex = new Regex(
                        '^' + Pattern + '$',
                        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
                        Constants.RegexMatchTimeout);
                }

                return _searchRegex;
            }
        }

        public bool ActiveChangeCallbacks => true;

        public bool HasChanged => TokenSource.Token.IsCancellationRequested;

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
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