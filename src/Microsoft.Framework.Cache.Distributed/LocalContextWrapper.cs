// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Cache.Memory;

namespace Microsoft.Framework.Cache.Distributed
{
    internal class LocalContextWrapper : ICacheContext
    {
        private readonly ICacheSetContext _context;
        private readonly MemoryStream _data = new MemoryStream();

        internal LocalContextWrapper(ICacheSetContext context)
        {
            _context = context;
        }

        public string Key
        {
            get { return _context.Key; }
        }

        public object State
        {
            get { return _context.State; }
        }

        public Stream Data
        {
            get { return _data; }
        }

        public void SetAbsoluteExpiration(TimeSpan relative)
        {
            _context.SetAbsoluteExpiration(relative);
        }

        public void SetAbsoluteExpiration(DateTimeOffset absolute)
        {
            _context.SetAbsoluteExpiration(absolute);
        }

        public void SetSlidingExpiration(TimeSpan relative)
        {
            _context.SetSlidingExpiration(relative);
        }

        internal byte[] GetBytes()
        {
            return _data.ToArray();
        }
    }
}