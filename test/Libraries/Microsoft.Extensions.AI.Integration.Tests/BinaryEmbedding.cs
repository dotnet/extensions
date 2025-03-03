// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

internal sealed class BinaryEmbedding : Embedding
{
    public BinaryEmbedding(ReadOnlyMemory<byte> bits)
    {
        Bits = bits;
    }

    public ReadOnlyMemory<byte> Bits { get; }
}
