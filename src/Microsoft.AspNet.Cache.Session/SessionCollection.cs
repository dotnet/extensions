using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.Cache.Session
{
    public class SessionCollection : ISessionCollection
    {
        private IDictionary<string, byte[]> _store = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        public bool IsModified { get; set; }

        public int Count { get { return _store.Count; } }

        public byte[] this[string key]
        {
            get { return Get(key); }
            set { Set(key, new ArraySegment<byte>(value)); }
        }

        public byte[] Get(string key)
        {
            byte[] value = null;
            _store.TryGetValue(key, out value);
            return value;
        }

        public void Set(string key, ArraySegment<byte> value)
        {
            IsModified = true;
            byte[] copy = new byte[value.Count];
            Buffer.BlockCopy(value.Array, value.Offset, copy, 0, value.Count);
            _store[key] = copy;
        }

        public void Clear()
        {
            IsModified |= Count > 0;
            _store.Clear();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _store.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, byte[]>> GetEnumerator()
        {
            return _store.GetEnumerator();
        }
    }
}