// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.VectorData.ProviderServices;

/// <summary>
/// Provides a mechanism for creating records.
/// </summary>
internal interface IRecordCreator
{
    /// <summary>
    /// Creates a new record of the specified type.
    /// </summary>
    /// <typeparam name="TRecord">The type of the record to create.</typeparam>
    /// <returns>A new instance of the specified record type.</returns>
    TRecord Create<TRecord>();
}
