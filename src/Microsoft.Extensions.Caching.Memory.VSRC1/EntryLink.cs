// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    public class EntryLink : IEntryLink
    {
        private readonly List<IChangeToken> _expirationTokens = new List<IChangeToken>();
        private bool _disposed;

        public EntryLink()
            : this(parent: null)
        {
        }

        public EntryLink(EntryLink parent)
        {
            Parent = parent;
        }

        public EntryLink Parent { get; }

        public DateTimeOffset? AbsoluteExpiration { get; private set; }

        public IEnumerable<IChangeToken> ExpirationTokens => _expirationTokens;

        public void AddExpirationTokens(IList<IChangeToken> expirationTokens)
        {
            if (expirationTokens == null)
            {
                throw new ArgumentNullException(nameof(expirationTokens));
            }

            _expirationTokens.AddRange(expirationTokens);
        }

        public void SetAbsoluteExpiration(DateTimeOffset absoluteExpiration)
        {
            if (!AbsoluteExpiration.HasValue)
            {
                AbsoluteExpiration = absoluteExpiration;
            }
            else if (absoluteExpiration < AbsoluteExpiration.Value)
            {
                AbsoluteExpiration = absoluteExpiration;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                EntryLinkHelpers.DisposeLinkingScope();
            }
        }
    }
}