// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal class VsEnumBSTR : IVsEnumBSTR
    {
        // Internal for testing
        internal readonly IReadOnlyList<string> _values;

        private int _currentIndex;

        public VsEnumBSTR(IReadOnlyList<string> values)
        {
            _values = values;
            _currentIndex = 0;
        }

        public int Clone(out IVsEnumBSTR ppEnum)
        {
            ppEnum = new VsEnumBSTR(_values);
            return VSConstants.S_OK;
        }

        public int GetCount(out uint pceltCount)
        {
            pceltCount = (uint)_values.Count;
            return VSConstants.S_OK;
        }

        public int Next(uint celt, string[] rgelt, out uint pceltFetched)
        {
            var i = 0;
            for (; i < celt && _currentIndex < _values.Count; i++, _currentIndex++)
            {
                rgelt[i] = _values[_currentIndex];
            }

            pceltFetched = (uint)i;
            return i < celt
                ? VSConstants.S_FALSE
                : VSConstants.S_OK;
        }

        public int Reset()
        {
            _currentIndex = 0;
            return VSConstants.S_OK;
        }

        public int Skip(uint celt)
        {
            _currentIndex += (int)celt;
            return _currentIndex < _values.Count
                ? VSConstants.S_OK
                : VSConstants.S_FALSE;
        }
    }
}
