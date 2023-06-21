// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Telemetry.Logging;

public partial class LoggerMessageState
{
    private sealed class PropertyCollector : ILogPropertyCollector
    {
        private const string Separator = "_";

        private readonly List<KeyValuePair<string, object?>> _properties;
        private readonly List<ClassifiedProperty> _classifiedProperties;

        public PropertyCollector(List<KeyValuePair<string, object?>> properties, List<ClassifiedProperty> classifiedProperties)
        {
            _properties = properties;
            _classifiedProperties = classifiedProperties;
        }

        public void Add(string propertyName, object? propertyValue)
        {
            string fullName = ParameterName.Length > 0 ? ParameterName + Separator + propertyName : propertyName;
            _properties.Add(new KeyValuePair<string, object?>(fullName, propertyValue));
        }

        public void Add(string propertyName, object? propertyValue, DataClassification classification)
        {
            string fullName = ParameterName.Length > 0 ? ParameterName + Separator + propertyName : propertyName;
            _classifiedProperties.Add(new(fullName, propertyValue, classification));
        }

        public string ParameterName { get; set; } = string.Empty;
    }
}
