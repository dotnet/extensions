using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Cache.Session
{
    [AssemblyNeutral]
    public interface ISessionCollection : IEnumerable<KeyValuePair<string, byte[]>>
    {
        byte[] this[string key] { get; set; }

        byte[] Get(string key);

        void Set(string key, ArraySegment<byte> value);

        void Clear();
    }
}