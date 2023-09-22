// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public sealed class CircularBufferTests
{
    [Fact]
    public void Constructor_InvalidSizePassed_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<int>(0, 0));
    }

    [Fact]
    public void GetFirstAndLastFromWindow_NoElements_DefaultsReturned()
    {
        const int DefaultElement = 5;
        var buffer = new CircularBuffer<int>(2, DefaultElement);

        var (firstElement, lastElement) = buffer.GetFirstAndLastFromWindow(2);
        Assert.Equal(DefaultElement, firstElement);
        Assert.Equal(DefaultElement, lastElement);
    }

    [Fact]
    public void GetFirstAndLastFromWindow_MoreElementsThanSizeAdded_RecentElementsReturned()
    {
        const int BufferSize = 5;
        var elementsToAdd = Enumerable.Range(1, BufferSize * 3).ToList();
        var expectedBufferElements = elementsToAdd.Skip(Math.Max(0, elementsToAdd.Count - BufferSize)).ToList();

        var buffer = new CircularBuffer<int>(BufferSize, 0);
        foreach (var element in elementsToAdd)
        {
            buffer.Add(element);
        }

        var (firstElement, lastElement) = buffer.GetFirstAndLastFromWindow(BufferSize);
        Assert.Equal(expectedBufferElements.First(), firstElement);
        Assert.Equal(expectedBufferElements.Last(), lastElement);
    }

    [Fact]
    public void GetFirstAndLastFromWindow_ProvidedBufferSizeGreaterThanActualBufferLength()
    {
        const int BufferSize = 5;
        var bufferElements = Enumerable.Range(1, BufferSize).ToList();
        var buffer = new CircularBuffer<int>(BufferSize, 0);

        foreach (var element in bufferElements)
        {
            buffer.Add(element);
        }

        var (firstElement, lastElement) = buffer.GetFirstAndLastFromWindow(BufferSize * 5);
        Assert.Equal(bufferElements[0], firstElement);
        Assert.Equal(bufferElements[4], lastElement);
    }

    [Fact]
    public void GetFirstAndLastFromWindow_ProvidedBufferSizeSmallerThanActualBufferLength()
    {
        const int BufferSize = 5;
        const int RequestedBufferSize = 3;

        var bufferElements = Enumerable.Range(1, BufferSize).ToList();
        var buffer = new CircularBuffer<int>(BufferSize, 0);

        foreach (var element in bufferElements)
        {
            buffer.Add(element);
        }

        // Requesting 1st and 2nd items using a window of size 3, should returns
        // the 3rd and 5th elements from the internal buffer.
        var (firstElement, lastElement) = buffer.GetFirstAndLastFromWindow(RequestedBufferSize);
        Assert.Equal(bufferElements[2], firstElement);
        Assert.Equal(bufferElements[4], lastElement);
    }
}
