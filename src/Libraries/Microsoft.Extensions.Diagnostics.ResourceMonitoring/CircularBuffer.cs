// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

internal sealed class CircularBuffer<T>
{
    private const int BufferSizeMultiplier = 2;
    private readonly T[] _elements;
    private readonly int _maxDistance;
    private int _bufferCursor;

    public CircularBuffer(int size, T defaultElement)
    {
        _ = Throw.IfLessThan(size, 1);

        _elements = new T[size * BufferSizeMultiplier];

        _maxDistance = size;

        for (var i = 0; i < _elements.Length; i++)
        {
            _elements[i] = defaultElement;
        }
    }

    public (T firstElement, T lastElement) GetFirstAndLastFromWindow(int distance)
    {
        if (distance > _maxDistance)
        {
            distance = _maxDistance;
        }

        var lastElementCursor = _bufferCursor;
        var firstElementCursor = lastElementCursor - distance + 1;
        if (firstElementCursor < 0)
        {
            firstElementCursor += _elements.Length;
        }

        return (_elements[firstElementCursor], _elements[lastElementCursor]);
    }

    public void Add(T newElement)
    {
        var cursor = (_bufferCursor + 1) % _elements.Length;
        _elements[cursor] = newElement;
        _bufferCursor = cursor;
    }
}
