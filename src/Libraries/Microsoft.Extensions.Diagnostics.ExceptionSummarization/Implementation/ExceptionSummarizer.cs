// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Text;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

/// <summary>
/// Looks through all the registered summary providers, returns a summary if possible.
/// </summary>
internal sealed class ExceptionSummarizer : IExceptionSummarizer
{
    private const string DefaultDescription = "Unknown";
    private readonly FrozenDictionary<Type, IExceptionSummaryProvider> _exceptionTypesToProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionSummarizer"/> class.
    /// </summary>
    /// <param name="providers">All registered exception providers.</param>
    public ExceptionSummarizer(IEnumerable<IExceptionSummaryProvider> providers)
    {
        var exceptionTypesToProvidersBuilder = new Dictionary<Type, IExceptionSummaryProvider>();
        foreach (var exceptionSummaryProvider in providers)
        {
            foreach (var exceptionType in exceptionSummaryProvider.SupportedExceptionTypes)
            {
                exceptionTypesToProvidersBuilder.Add(exceptionType, exceptionSummaryProvider);
            }
        }

        _exceptionTypesToProviders = exceptionTypesToProvidersBuilder.ToFrozenDictionary(optimizeForReading: true);
    }

    /// <summary>
    /// It iterates through all registered summarizers, returns a summary if possible.
    /// Default is <see cref="Exception"/> message if its length is less than 32, otherwise exception type name.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The summary of the given <see cref="Exception"/>.</returns>
    public ExceptionSummary Summarize(Exception exception)
    {
        _ = Throw.IfNull(exception);
        var exceptionType = exception.GetType();
        var exceptionTypeName = exception.GetType().Name;

        if (_exceptionTypesToProviders.TryGetValue(exceptionType, out var exceptionSummaryProvider))
        {
            return BuildSummary(exception, exceptionSummaryProvider, exceptionTypeName);
        }

        // Let's see if we get lucky with the inner exception
        if (exception.InnerException != null)
        {
            var innerExceptionType = exception.InnerException.GetType();
            if (_exceptionTypesToProviders.TryGetValue(innerExceptionType, out var innerExceptionSummaryProvider))
            {
                return BuildSummary(
                    exception.InnerException,
                    innerExceptionSummaryProvider,
                    $"{exceptionTypeName}->{innerExceptionType.Name}");
            }
        }

        // Now let's see if we can get something from Exception HResult
        var hresult = exception.HResult;
        var exceptionDescription = exception.InnerException != null
            ? exception.InnerException.GetType().Name
            : DefaultDescription;
        if (hresult != default)
        {
            return new ExceptionSummary(
                exceptionTypeName,
                exceptionDescription,
                hresult.ToInvariantString());
        }

        // final recourse, generate a default message
        return new ExceptionSummary(
            exceptionTypeName,
            exceptionDescription,
            DefaultDescription);
    }

    private static ExceptionSummary BuildSummary(
        Exception exception,
        IExceptionSummaryProvider exceptionSummaryProvider,
        string exceptionType)
    {
        var descriptionIndex = exceptionSummaryProvider.Describe(exception, out var additionalDetails);

        if (descriptionIndex >= exceptionSummaryProvider.Descriptions.Count || descriptionIndex < 0)
        {
            return new ExceptionSummary(
                exceptionType,
                DefaultDescription,
                $"Exception summary provider {exceptionSummaryProvider.GetType().Name} returned invalid short description index {descriptionIndex}");
        }

        return new ExceptionSummary(exceptionType, exceptionSummaryProvider.Descriptions[descriptionIndex], additionalDetails ?? DefaultDescription);
    }
}
