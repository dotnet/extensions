// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Primitives
{
    public struct CorrelationId : IComparable<CorrelationId>, IEquatable<CorrelationId>
    {
        // Base32 encoding - in ascii sort order for easy text based sorting
        private const string _encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

        private long _id;
        private string _stringId;

        public long Id => _id;

        internal CorrelationId(long correlationId)
        {
            _id = correlationId;
            _stringId = null;
        }

        public override string ToString() => _stringId ?? GenerateCorrelationString();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal unsafe string GenerateCorrelationString()
        {
            // The following routine is ~310% faster than calling long.ToString() on x64
            // and ~600% faster than calling long.ToString() on x86 in tight loops of 1 million+ iterations
            // See: https://github.com/aspnet/Hosting/pull/385

            var id = _id;
            // stackalloc to allocate array on stack rather than heap
            char* charBuffer = stackalloc char[13];

            charBuffer[0] = _encode32Chars[(int)(id >> 60) & 31];
            charBuffer[1] = _encode32Chars[(int)(id >> 55) & 31];
            charBuffer[2] = _encode32Chars[(int)(id >> 50) & 31];
            charBuffer[3] = _encode32Chars[(int)(id >> 45) & 31];
            charBuffer[4] = _encode32Chars[(int)(id >> 40) & 31];
            charBuffer[5] = _encode32Chars[(int)(id >> 35) & 31];
            charBuffer[6] = _encode32Chars[(int)(id >> 30) & 31];
            charBuffer[7] = _encode32Chars[(int)(id >> 25) & 31];
            charBuffer[8] = _encode32Chars[(int)(id >> 20) & 31];
            charBuffer[9] = _encode32Chars[(int)(id >> 15) & 31];
            charBuffer[10] = _encode32Chars[(int)(id >> 10) & 31];
            charBuffer[11] = _encode32Chars[(int)(id >> 5) & 31];
            charBuffer[12] = _encode32Chars[(int)id & 31];

            // string ctor overload that takes char*
            var s = new string(charBuffer, 0, 13);
            _stringId = s;
            return s;
        }

        public override int GetHashCode() => Id.GetHashCode();

        public int CompareTo(CorrelationId other) => Id.CompareTo(other.Id);

        public bool Equals(CorrelationId other) => Id.Equals(other.Id);
    }
}
