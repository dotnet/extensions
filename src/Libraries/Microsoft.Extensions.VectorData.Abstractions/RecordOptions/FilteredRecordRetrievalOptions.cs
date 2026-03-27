// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.VectorData;

/// <summary>
/// Defines options for calling <see cref="VectorStoreCollection{TKey, TRecord}.GetAsync(Expression{Func{TRecord, bool}}, int, FilteredRecordRetrievalOptions{TRecord}, CancellationToken)"/>.
/// </summary>
/// <typeparam name="TRecord">The type of the record.</typeparam>
public sealed class FilteredRecordRetrievalOptions<TRecord>
{
    /// <summary>
    /// Gets or sets the number of results to skip before returning results, that is, the index of the first result to return.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 0.</exception>
    public int Skip
    {
        get => field;
        set
        {
            if (value < 0)
            {
                Throw.ArgumentOutOfRangeException(nameof(value), "Skip must be greater than or equal to 0.");
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the data property to order by.
    /// </summary>
    /// <value>
    /// If not provided, the order of returned results is non-deterministic.
    /// </value>
    public Func<OrderByDefinition, OrderByDefinition>? OrderBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include vectors in the retrieval result.
    /// </summary>
    public bool IncludeVectors { get; set; }

    /// <summary>
    /// Represents a builder for sorting.
    /// </summary>
    // This type does not derive any collection in order to avoid Intellisense suggesting LINQ methods.
    public sealed class OrderByDefinition
    {
        private readonly List<SortInfo> _values = [];

        /// <summary>
        /// Gets the expressions to sort by.
        /// </summary>
        /// <remarks>This property is intended to be consumed by the connectors to retrieve the configuration.</remarks>
        public IReadOnlyList<SortInfo> Values => _values;

        /// <summary>
        /// Creates an ascending sort.
        /// </summary>
        /// <returns>The current <see cref="OrderByDefinition"/> for chaining.</returns>
        public OrderByDefinition Ascending(Expression<Func<TRecord, object?>> propertySelector)
        {
            if (propertySelector is null)
            {
                Throw.ArgumentNullException(nameof(propertySelector));
            }

            _values.Add(new(propertySelector, true));
            return this;
        }

        /// <summary>
        /// Creates a descending sort.
        /// </summary>
        /// <returns>The current <see cref="OrderByDefinition"/> for chaining.</returns>
        public OrderByDefinition Descending(Expression<Func<TRecord, object?>> propertySelector)
        {
            if (propertySelector is null)
            {
                Throw.ArgumentNullException(nameof(propertySelector));
            }

            _values.Add(new(propertySelector, false));
            return this;
        }

        /// <summary>
        /// Provides a way to define property ordering.
        /// </summary>
        /// <remarks>This class is intended to be consumed by the connectors to retrieve the configuration.</remarks>
        public sealed class SortInfo
        {
            internal SortInfo(Expression<Func<TRecord, object?>> propertySelector, bool isAscending)
            {
                PropertySelector = propertySelector;
                Ascending = isAscending;
            }

            /// <summary>
            /// Gets the expression to select the property to sort by.
            /// </summary>
            public Expression<Func<TRecord, object?>> PropertySelector { get; }

            /// <summary>
            /// Gets a value indicating whether the sort is ascending; otherwise, false.
            /// </summary>
            /// <value>
            /// <see langword="true"/> if the sort is ascending; otherwise, <see langword="false"/>.
            /// </value>
            public bool Ascending { get; }
        }
    }
}
