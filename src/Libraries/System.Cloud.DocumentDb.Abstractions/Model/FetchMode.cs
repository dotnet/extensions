// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Cloud.DocumentDb;

/// <summary>
/// Fetch condition of query.
/// </summary>
public enum FetchMode
{
    /// <summary>
    /// Indicating whether we should fetch all documents.
    /// </summary>
    FetchAll,

    /// <summary>
    /// Indicating whether we should fetch only one page of results.
    /// </summary>
    /// <remarks>
    /// Page represents a physical group of data located on specific machine.
    /// The page should represent a partition,
    /// in a case of cross partition fetch each call will return data of a single partition.
    /// If a database implementation allows to distribute a partition data across servers,
    /// this page can be a subset of partition.
    /// </remarks>
    FetchSinglePage,

    /// <summary>
    /// Indicating whether we should ensure fetching at least the number of max item count.
    /// </summary>
    /// <remarks>
    /// This parameter should only being served on cross partition query.
    /// For instance, if you set the <see cref="QueryRequestOptions{TDocument}.MaxResults"/> to 50.
    /// On in partition query, it will return you exactly 50 items if there is that much.
    /// But for cross partition query, it might return you only 30 items on a single fetch.
    /// In a case of <see cref="FetchSinglePage"/> only 30 items will be returned with a continuation token to
    /// let you fetch forward.
    /// In a case of <see cref="FetchMaxResults"/>, another round of single fetch query will be requested with same
    /// <see cref="QueryRequestOptions{TDocument}.MaxResults"/>, which means 80 items at maximum can be returned.
    /// </remarks>
    FetchMaxResults,
}
