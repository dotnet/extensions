// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable SA1405 // Debug.Assert should provide message text
#pragma warning disable IDE0032 // Use auto property
#pragma warning disable S3358 // Ternary operators should not be nested
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable S109 // Magic numbers should not be used

namespace System.Net
{
    // Warning: Mutable struct!
    // The purpose of this struct is to simplify buffer management.
    // It manages a sliding buffer where bytes can be added at the end and removed at the beginning.
    // [ActiveSpan/Memory] contains the current buffer contents; these bytes will be preserved
    // (copied, if necessary) on any call to EnsureAvailableBytes.
    // [AvailableSpan/Memory] contains the available bytes past the end of the current content,
    // and can be written to in order to add data to the end of the buffer.
    // Commit(byteCount) will extend the ActiveSpan by [byteCount] bytes into the AvailableSpan.
    // Discard(byteCount) will discard [byteCount] bytes as the beginning of the ActiveSpan.

    [StructLayout(LayoutKind.Auto)]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal struct ArrayBuffer : IDisposable
    {
        private readonly bool _usePool;
        private byte[] _bytes;
        private int _activeStart;
        private int _availableStart;

        // Invariants:
        // 0 <= _activeStart <= _availableStart <= bytes.Length

        public ArrayBuffer(int initialSize, bool usePool = false)
        {
            Debug.Assert(initialSize > 0 || usePool);

            _usePool = usePool;
            _bytes = initialSize == 0
                ? Array.Empty<byte>()
                : usePool ? ArrayPool<byte>.Shared.Rent(initialSize) : new byte[initialSize];
            _activeStart = 0;
            _availableStart = 0;
        }

        public ArrayBuffer(byte[] buffer)
        {
            Debug.Assert(buffer.Length > 0);

            _usePool = false;
            _bytes = buffer;
            _activeStart = 0;
            _availableStart = 0;
        }

        public void Dispose()
        {
            _activeStart = 0;
            _availableStart = 0;

            byte[] array = _bytes;
            _bytes = null!;

            if (array is not null)
            {
                ReturnBufferIfPooled(array);
            }
        }

        // This is different from Dispose as the instance remains usable afterwards (_bytes will not be null).
        public void ClearAndReturnBuffer()
        {
            Debug.Assert(_usePool);
            Debug.Assert(_bytes is not null);

            _activeStart = 0;
            _availableStart = 0;

            byte[] bufferToReturn = _bytes!;
            _bytes = Array.Empty<byte>();
            ReturnBufferIfPooled(bufferToReturn);
        }

        public readonly int ActiveLength => _availableStart - _activeStart;
        public readonly Span<byte> ActiveSpan => new Span<byte>(_bytes, _activeStart, _availableStart - _activeStart);
        public readonly ReadOnlySpan<byte> ActiveReadOnlySpan => new ReadOnlySpan<byte>(_bytes, _activeStart, _availableStart - _activeStart);
        public readonly Memory<byte> ActiveMemory => new Memory<byte>(_bytes, _activeStart, _availableStart - _activeStart);

        public readonly int AvailableLength => _bytes.Length - _availableStart;
        public readonly Span<byte> AvailableSpan => _bytes.AsSpan(_availableStart);
        public readonly Memory<byte> AvailableMemory => _bytes.AsMemory(_availableStart);
        public readonly Memory<byte> AvailableMemorySliced(int length) => new Memory<byte>(_bytes, _availableStart, length);

        public readonly int Capacity => _bytes.Length;
        public readonly int ActiveStartOffset => _activeStart;

        public readonly byte[] DangerousGetUnderlyingBuffer() => _bytes;

        public void Discard(int byteCount)
        {
            Debug.Assert(byteCount <= ActiveLength, $"Expected {byteCount} <= {ActiveLength}");
            _activeStart += byteCount;

            if (_activeStart == _availableStart)
            {
                _activeStart = 0;
                _availableStart = 0;
            }
        }

        public void Commit(int byteCount)
        {
            Debug.Assert(byteCount <= AvailableLength);
            _availableStart += byteCount;
        }

        // Ensure at least [byteCount] bytes to write to.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureAvailableSpace(int byteCount)
        {
            if (byteCount > AvailableLength)
            {
                EnsureAvailableSpaceCore(byteCount);
            }
        }

        private void EnsureAvailableSpaceCore(int byteCount)
        {
            Debug.Assert(AvailableLength < byteCount);

            if (_bytes.Length == 0)
            {
                Debug.Assert(_usePool && _activeStart == 0 && _availableStart == 0);
                _bytes = ArrayPool<byte>.Shared.Rent(byteCount);
                return;
            }

            int totalFree = _activeStart + AvailableLength;
            if (byteCount <= totalFree)
            {
                // We can free up enough space by just shifting the bytes down, so do so.
                Buffer.BlockCopy(_bytes, _activeStart, _bytes, 0, ActiveLength);
                _availableStart = ActiveLength;
                _activeStart = 0;
                Debug.Assert(byteCount <= AvailableLength);
                return;
            }

            // Double the size of the buffer until we have enough space.
            int desiredSize = ActiveLength + byteCount;
            int newSize = _bytes.Length;
            do
            {
                newSize *= 2;
            }
            while (newSize < desiredSize);

            byte[] newBytes = _usePool ?
                ArrayPool<byte>.Shared.Rent(newSize) :
                new byte[newSize];
            byte[] oldBytes = _bytes;

            if (ActiveLength != 0)
            {
                Buffer.BlockCopy(oldBytes, _activeStart, newBytes, 0, ActiveLength);
            }

            _availableStart = ActiveLength;
            _activeStart = 0;

            _bytes = newBytes;
            ReturnBufferIfPooled(oldBytes);

            Debug.Assert(byteCount <= AvailableLength);
        }

        public void Grow()
        {
            EnsureAvailableSpaceCore(AvailableLength + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void ReturnBufferIfPooled(byte[] buffer)
        {
            // The buffer may be Array.Empty<byte>()
            if (_usePool && buffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
