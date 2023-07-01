// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Telemetry.Logging;

internal sealed partial class ExtendedLogger
{
    private sealed class Scope : IDisposable
    {
        private const int NumInlineDisposables = 2;

        private readonly IDisposable?[]? _disposable;
        private bool _isDisposed;
        private IDisposable? _disposable0;
        private IDisposable? _disposable1;

        public Scope(int count)
        {
            if (count > NumInlineDisposables)
            {
                _disposable = new IDisposable[count - NumInlineDisposables];
            }
        }

        public void SetDisposable(int index, IDisposable? disposable)
        {
            switch (index)
            {
                case 0:
                    _disposable0 = disposable;
                    break;
                case 1:
                    _disposable1 = disposable;
                    break;
                default:
                    _disposable![index - NumInlineDisposables] = disposable;
                    break;
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _disposable0?.Dispose();
                _disposable1?.Dispose();

                if (_disposable != null)
                {
                    int count = _disposable.Length;
                    for (int index = 0; index != count; ++index)
                    {
                        _disposable[index]?.Dispose();
                    }
                }

                _isDisposed = true;
            }
        }
    }
}
