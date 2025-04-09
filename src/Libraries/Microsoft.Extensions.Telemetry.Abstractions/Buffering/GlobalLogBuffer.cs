﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Buffers logs into global circular buffers and drops them after some time if not flushed.
/// </summary>
public abstract class GlobalLogBuffer : LogBuffer
{
}

#endif
