using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.Cache.Session
{
    public class SessionCollection : ISessionCollection
    {
        private readonly Func<bool> _tryEstablishSession;
        private IDictionary<string, byte[]> _store = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        public SessionCollection([NotNull] Func<bool> tryEstablishSession)
        {
            _tryEstablishSession = tryEstablishSession;
        }

        public bool IsModified { get; set; }

        public int Count { get { return _store.Count; } }

        public byte[] this[string key]
        {
            get
            {
                byte[] value;
                TryGetValue(key, out value);
                return value;
            }
            set
            {
                if (value == null)
                {
                    Remove(key);
                }
                else
                {
                    Set(key, new ArraySegment<byte>(value));
                }
            }
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _store.TryGetValue(key, out value);
        }

        public void Set(string key, ArraySegment<byte> value)
        {
            // TODO: Validate arguments. Non-null array.
            if (!_tryEstablishSession())
            {
                throw new InvalidOperationException("The session cannot be established after the response has started.");
            }
            IsModified = true;
            byte[] copy = new byte[value.Count];
            Buffer.BlockCopy(value.Array, value.Offset, copy, 0, value.Count);
            _store[key] = copy;
        }

        public void SetInternal(string key, byte[] value)
        {
            _store[key] = value;
        }

        public void Remove(string key)
        {
            IsModified |= _store.Remove(key);
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