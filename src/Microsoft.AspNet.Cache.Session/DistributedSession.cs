// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNet.HttpFeature;
using Microsoft.Framework.Cache.Distributed;

namespace Microsoft.AspNet.Cache.Session
{
    public class DistributedSession : ISession
    {
        private const byte SerializationRevision = 1;

        private readonly IDistributedCache _cache;
        private readonly string _key;
        private readonly TimeSpan _idleTimeout;
        private readonly Func<bool> _tryEstablishSession;
        private readonly IDictionary<string, byte[]> _store;
        private bool _isModified;
        private bool _loaded;

        public DistributedSession([NotNull] IDistributedCache cache, [NotNull] string key, TimeSpan idleTimeout, [NotNull] Func<bool> tryEstablishSession)
        {
            _cache = cache;
            _key = key;
            _idleTimeout = idleTimeout;
            _tryEstablishSession = tryEstablishSession;
            _store = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        }

        public IEnumerable<string> Keys
        {
            get
            {
                Load(); // TODO: Silent failure
                return _store.Keys;
            }
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            Load(); // TODO: Silent failure
            return _store.TryGetValue(key, out value);
        }

        public void Set(string key, ArraySegment<byte> value)
        {
            Load();
            // TODO: Validate arguments. Non-null array.
            if (!_tryEstablishSession())
            {
                throw new InvalidOperationException("The session cannot be established after the response has started.");
            }
            _isModified = true;
            byte[] copy = new byte[value.Count];
            Buffer.BlockCopy(value.Array, value.Offset, copy, 0, value.Count);
            _store[key] = copy;
        }

        public void Remove(string key)
        {
            Load();
            _isModified |= _store.Remove(key);
        }

        public void Clear()
        {
            Load();
            _isModified |= _store.Count > 0;
            _store.Clear();
        }

        // TODO: This should throw if called directly, but most other places it should fail silently (e.g. TryGetValue should just return null).
        public void Load()
        {
            if (!_loaded)
            {
                byte[] data;
                if (_cache.TryGetValue(_key, out data))
                {
                    Deserialize(data);
                }
                _loaded = true;
            }
        }

        public void Commit()
        {
            if (_isModified)
            {
                _isModified = false;
                _cache.Set(_key, context => {
                    context.SetSlidingExpiration(_idleTimeout);
                    return Serialize();
                });
            }
        }

        // Format:
        // byte (1, 0-255): Serialization revision
        // umed (3, 0-16m): entry count
        // foreach entry:
        //  ushort (2, 0-64k): key name byte length
        //  byte[]: utf-8 encoded key name
        //  uint (4, 0-4b): data byte length
        //  byte[]: data
        private byte[] Serialize()
        {
            var builder = new BufferBuilder();
            builder.Add(SerializationRevision);
            builder.Add(SerializeNumAs3Bytes(_store.Count));

            foreach (var entry in _store)
            {
                var serializedKey = Encoding.UTF8.GetBytes(entry.Key);
                builder.Add(SerializeNumAs2Bytes(serializedKey.Length));
                builder.Add(serializedKey);
                builder.Add(SerializeNumAs4Bytes(entry.Value.Length));
                builder.Add(entry.Value);
            }

            return builder.Build();
        }

        private void Deserialize(byte[] content)
        {
            if (content == null || content.Length < 4 || content[0] != SerializationRevision)
            {
                // TODO: Throw?
                // Replace the un-readable format.
                _isModified = true;
                return;
            }

            int offset = 1;
            int expectedEntries = DeserializeNumFrom3Bytes(content, offset: ref offset);
            for (int i = 0; i < expectedEntries; i++)
            {
                int keyLength = DeserializeNumFrom2Bytes(content, offset: ref offset);
                var key = Encoding.UTF8.GetString(content, offset, keyLength);
                offset += keyLength;
                int dataLength = DeserializeNumFrom4Bytes(content, offset: ref offset);
                byte[] data = new byte[dataLength];
                Buffer.BlockCopy(content, offset, data, 0, dataLength);
                offset += dataLength;
                _store[key] = data;
            }

            Debug.Assert(offset == content.Length, "De-serialization length mismatch");
        }

        private byte[] SerializeNumAs2Bytes(int num)
        {
            if (num < 0 || ushort.MaxValue < num)
            {
                throw new ArgumentOutOfRangeException("num", num, "The value cannot be serialized in two bytes.");
            }
            return new byte[]
            {
                (byte)(num >> 8),
                (byte)(0xFF & num)
            };
        }

        private int DeserializeNumFrom2Bytes(byte[] content, ref int offset)
        {
            if (content.Length - offset < 2)
            {
                throw new ArgumentException("Insufficient data remaining", "content");
            }
            return content[offset++] << 8 | content[offset++];
        }

        private byte[] SerializeNumAs3Bytes(int num)
        {
            if (num < 0 || 0xFFFFFF < num)
            {
                throw new ArgumentOutOfRangeException("num", num, "The value cannot be serialized in three bytes.");
            }
            return new byte[]
            {
                (byte)(num >> 16),
                (byte)(0xFF & (num >> 8)),
                (byte)(0xFF & num)
            };
        }

        private int DeserializeNumFrom3Bytes(byte[] content, ref int offset)
        {
            if (content.Length - offset < 3)
            {
                throw new ArgumentException("Insufficient data remaining", "content");
            }
            return content[offset++] << 16 | content[offset++] << 8 | content[offset++];
        }

        private byte[] SerializeNumAs4Bytes(int num)
        {
            if (num < 0)
            {
                throw new ArgumentOutOfRangeException("num", num, "The value cannot be negative.");
            }
            return new byte[]
            {
                (byte)(num >> 24),
                (byte)(0xFF & (num >> 16)),
                (byte)(0xFF & (num >> 8)),
                (byte)(0xFF & num)
            };
        }

        private int DeserializeNumFrom4Bytes(byte[] content, ref int offset)
        {
            if (content.Length - offset < 4)
            {
                throw new ArgumentException("Insufficient data remaining", "content");
            }
            return content[offset++] << 24 | content[offset++] << 16 | content[offset++] << 8 | content[offset++];
        }
    }
}