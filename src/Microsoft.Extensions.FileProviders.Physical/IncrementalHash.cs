// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET451
using System;
using System.Security.Cryptography;

namespace Microsoft.Extensions.FileProviders.Physical
{
    internal class IncrementalHash : IDisposable
    {
        private static readonly byte[] EmptyArray = new byte[0];
        private readonly SHA256 _sha256 = CreateSHA256();
        private bool _initialized;

        public void AppendData(byte[] data, int offset, int count)
        {
            EnsureInitialized();
            _sha256.TransformBlock(data, offset, count, outputBuffer: null, outputOffset: 0);
        }

        public byte[] GetHashAndReset()
        {
            _sha256.TransformFinalBlock(EmptyArray, 0, 0);
            return _sha256.Hash;
        }

        public void Dispose()
        {
            _sha256.Dispose();
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                _sha256.Initialize();
                _initialized = true;
            }
        }

        private static SHA256 CreateSHA256()
        {
            SHA256 sha256;
            try
            {
                sha256 = SHA256.Create();
            }
            catch (System.Reflection.TargetInvocationException)
            {
                // SHA256.Create is documented to throw this exception on FIPS compliant machines.
                // See: https://msdn.microsoft.com/en-us/library/z08hz7ad%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
                // Fallback to a FIPS compliant SHA256 algorithm.
                sha256 = new SHA256CryptoServiceProvider();
            }

            return sha256;
        }
    }
}
#endif