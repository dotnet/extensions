// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DataIngestion;

internal class Envelope<T>
{
#pragma warning disable IDE1006 // Naming Styles
    public T? data { get; set; }
#pragma warning restore IDE1006 // Naming Styles
}
