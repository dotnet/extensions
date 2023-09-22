// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;

namespace Microsoft.Extensions.Http.Logging.Internal;

// The list of official media types can be found here https://en.wikipedia.org/wiki/Media_type#cite_note-10
// It is huge and we need better a way to verify allowed / disallowed media types.
// One approach could be memoization of incoming media types and extract this method into
// Http.Logging so LogHttp could also benefit and we prevent repetition.
internal static class MediaTypeCollectionExtensions
{
    private const string Application = "application";
    private const string Json = "+json";
    private const string Xml = "+xml";
    private const string Text = "text/";

    public static bool Covers(this FrozenSet<string> collection, string? sample)
    {
        if (!string.IsNullOrEmpty(sample))
        {
            if (collection.Contains(sample!))
            {
                return true;
            }

            if (sample!.StartsWith(Text, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (sample.StartsWith(Application, StringComparison.OrdinalIgnoreCase)
                && (sample.EndsWith(Json, StringComparison.OrdinalIgnoreCase)
                || sample.EndsWith(Xml, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
