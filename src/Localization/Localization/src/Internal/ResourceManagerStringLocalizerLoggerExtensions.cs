// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Localization.Internal
{
    internal static class ResourceManagerStringLocalizerLoggerExtensions
    {
        private static readonly LogMessage<string, string, CultureInfo> _searchedLocation;

        static ResourceManagerStringLocalizerLoggerExtensions()
        {
            _searchedLocation = (
                LogLevel.Debug,
                new EventId(1, "SearchedLocation"),
                $"{nameof(ResourceManagerStringLocalizer)} searched for '{{Key}}' in '{{LocationSearched}}' with culture '{{Culture}}'.");
        }

        public static void SearchedLocation(this ILogger logger, string key, string searchedLocation, CultureInfo culture)
        {
            _searchedLocation.Log(logger, key, searchedLocation, culture);
        }
    }
}
