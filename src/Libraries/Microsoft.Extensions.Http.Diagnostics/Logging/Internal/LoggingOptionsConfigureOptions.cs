// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Logging.Internal;

internal sealed class LoggingOptionsConfigureOptions : IConfigureNamedOptions<LoggingOptions>
{
    private static readonly DataClassificationTypeConverter _dataClassificationConverter = new();

    private readonly string? _name;
    private readonly IConfigurationSection _section;

    public LoggingOptionsConfigureOptions(IConfigurationSection section)
        : this(Microsoft.Extensions.Options.Options.DefaultName, section)
    {
    }

    public LoggingOptionsConfigureOptions(string? name, IConfigurationSection section)
    {
        _name = name;
        _section = Throw.IfNull(section);
    }

    public void Configure(LoggingOptions options)
    {
        _ = Throw.IfNull(options);

        Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
    }

    public void Configure(string? name, LoggingOptions options)
    {
        _ = Throw.IfNull(options);

        if (!string.Equals(name, _name, StringComparison.Ordinal) || !_section.Exists())
        {
            return;
        }

        BindValue(_section, nameof(LoggingOptions.LogRequestStart), bool.Parse, value => options.LogRequestStart = value);
        BindDataClassifications(_section.GetSection(nameof(LoggingOptions.RequestQueryParametersDataClasses)), options.RequestQueryParametersDataClasses);
        BindValue(_section, nameof(LoggingOptions.LogBody), bool.Parse, value => options.LogBody = value);
        BindValue(
            _section,
            nameof(LoggingOptions.BodySizeLimit),
            static value => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture),
            value => options.BodySizeLimit = value);
        BindValue(
            _section,
            nameof(LoggingOptions.BodyReadTimeout),
            static value => TimeSpan.Parse(value, CultureInfo.InvariantCulture),
            value => options.BodyReadTimeout = value);
        BindSet(_section.GetSection(nameof(LoggingOptions.RequestBodyContentTypes)), options.RequestBodyContentTypes);
        BindSet(_section.GetSection(nameof(LoggingOptions.ResponseBodyContentTypes)), options.ResponseBodyContentTypes);
        BindDataClassifications(_section.GetSection(nameof(LoggingOptions.RequestHeadersDataClasses)), options.RequestHeadersDataClasses);
        BindDataClassifications(_section.GetSection(nameof(LoggingOptions.ResponseHeadersDataClasses)), options.ResponseHeadersDataClasses);
        BindEnum<OutgoingPathLoggingMode>(_section, nameof(LoggingOptions.RequestPathLoggingMode), value => options.RequestPathLoggingMode = value);
        BindEnum<HttpRouteParameterRedactionMode>(_section, nameof(LoggingOptions.RequestPathParameterRedactionMode), value => options.RequestPathParameterRedactionMode = value);
        BindDataClassifications(_section.GetSection(nameof(LoggingOptions.RouteParameterDataClasses)), options.RouteParameterDataClasses);
        BindValue(_section, nameof(LoggingOptions.LogContentHeaders), bool.Parse, value => options.LogContentHeaders = value);
    }

    private static void BindSet(IConfigurationSection section, ISet<string> destination)
    {
        foreach (var child in section.GetChildren())
        {
            if (child.Value is string value)
            {
                _ = destination.Add(value);
            }
        }
    }

    private static void BindDataClassifications(IConfigurationSection section, IDictionary<string, DataClassification> destination)
    {
        foreach (var child in section.GetChildren())
        {
            if (TryParseDataClassification(child, out var classification))
            {
                destination[child.Key] = classification;
            }
        }
    }

    private static void BindEnum<TEnum>(IConfigurationSection section, string key, Action<TEnum> setter)
        where TEnum : struct
        => BindValue(section, key, static value => (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase: true), setter);

    private static void BindValue<T>(IConfigurationSection section, string key, Func<string, T> parser, Action<T> setter)
    {
        if (section[key] is string value)
        {
            setter(parser(value));
        }
    }

    private static bool TryParseDataClassification(IConfigurationSection section, out DataClassification classification)
    {
        if (section.Value is string value)
        {
            try
            {
                classification = (DataClassification)_dataClassificationConverter.ConvertFromInvariantString(value)!;
                return true;
            }
            catch (Exception)
            {
                classification = default;
                return false;
            }
        }

        var taxonomyName = section["taxonomyName"] ?? section[nameof(DataClassification.TaxonomyName)];
        var classificationValue = section["value"] ?? section[nameof(DataClassification.Value)];

        if (string.IsNullOrWhiteSpace(taxonomyName) || string.IsNullOrWhiteSpace(classificationValue))
        {
            classification = default;
            return false;
        }

        try
        {
            classification = new DataClassification(taxonomyName!, classificationValue!);
            return true;
        }
        catch (ArgumentException)
        {
            classification = default;
            return false;
        }
    }
}