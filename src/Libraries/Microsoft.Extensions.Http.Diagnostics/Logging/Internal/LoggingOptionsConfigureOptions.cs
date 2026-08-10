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
    private readonly string? _name;
    private readonly IConfigurationSection _section;

    public LoggingOptionsConfigureOptions(string? name, IConfigurationSection section)
    {
        _name = name;
        _section = Throw.IfNull(section);
    }

    public void Configure(LoggingOptions options) => Configure(global::Microsoft.Extensions.Options.Options.DefaultName, options);

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
        foreach (IConfigurationSection child in section.GetChildren())
        {
            if (child.Value is string value)
            {
                _ = destination.Add(value);
            }
        }
    }

    private static void BindDataClassifications(IConfigurationSection section, IDictionary<string, DataClassification> destination)
    {
        foreach (IConfigurationSection child in section.GetChildren())
        {
            destination[child.Key] = ParseDataClassification(child);
        }
    }

    private static DataClassification ParseDataClassification(IConfigurationSection section)
    {
        if (section.Value is string value)
        {
            try
            {
                if (value == nameof(DataClassification.None))
                {
                    return DataClassification.None;
                }

                if (value == nameof(DataClassification.Unknown))
                {
                    return DataClassification.Unknown;
                }

                int delimiterIndex = value.IndexOf(":", StringComparison.Ordinal);
                if (delimiterIndex > 0 && delimiterIndex < value.Length - 1)
                {
                    return new DataClassification(value.Substring(0, delimiterIndex), value.Substring(delimiterIndex + 1));
                }

                throw new FormatException($"Invalid data classification format: '{value}'.");
            }
            catch (Exception exception)
            {
                throw CreateBindingException(value, section.Path, typeof(DataClassification), exception);
            }
        }

        string? taxonomyName = section[nameof(DataClassification.TaxonomyName)];
        string? classificationValue = section[nameof(DataClassification.Value)];

        try
        {
            return new DataClassification(taxonomyName!, classificationValue!);
        }
        catch (Exception exception)
        {
            throw CreateBindingException(section.Value, section.Path, typeof(DataClassification), exception);
        }
    }

    private static void BindEnum<TEnum>(IConfigurationSection section, string key, Action<TEnum> setter)
        where TEnum : struct
        => BindValue(section, key, ParseEnum<TEnum>, setter);

    private static TEnum ParseEnum<TEnum>(string value)
        where TEnum : struct
        => Enum.TryParse(value, ignoreCase: true, out TEnum result)
            ? result
            : throw new FormatException($"'{value}' is not a valid value for {typeof(TEnum)}.");

    private static void BindValue<T>(IConfigurationSection section, string key, Func<string, T> parser, Action<T> setter)
    {
        IConfigurationSection valueSection = section.GetSection(key);
        if (valueSection.Value is not string value)
        {
            return;
        }

        try
        {
            setter(parser(value));
        }
        catch (Exception exception)
        {
            throw CreateBindingException(value, valueSection.Path, typeof(T), exception);
        }
    }

    private static InvalidOperationException CreateBindingException(string? value, string path, Type type, Exception innerException)
        => new($"Failed to convert configuration value '{value ?? "null"}' at '{path}' to type '{type}'.", innerException);
}