// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
#if !NET
using System.Linq;
#else
using System.Runtime.CompilerServices;
#endif

namespace Microsoft.Extensions.AI;

/// <summary>Internal extensions for working with <see cref="AIContent"/>.</summary>
internal static class AIContentExtensions
{
    /// <summary>Finds the first occurrence of a <typeparamref name="T"/> in the list.</summary>
    public static T? FindFirst<T>(this IList<AIContent> contents)
        where T : AIContent
    {
        int count = contents.Count;
        for (int i = 0; i < count; i++)
        {
            if (contents[i] is T t)
            {
                return t;
            }
        }

        return null;
    }

    /// <summary>Concatenates the text of all <see cref="TextContent"/> instances in the list.</summary>
    public static string ConcatText(this IList<AIContent> contents)
    {
        int count = contents.Count;
        switch (count)
        {
            case 0:
                break;

            case 1:
                return contents[0] is TextContent tc ? tc.Text : string.Empty;

            default:
#if NET
                DefaultInterpolatedStringHandler builder = new(0, 0, null, stackalloc char[512]);
                for (int i = 0; i < count; i++)
                {
                    if (contents[i] is TextContent text)
                    {
                        builder.AppendLiteral(text.Text);
                    }
                }

                return builder.ToStringAndClear();
#else
                return string.Concat(contents.OfType<TextContent>());
#endif
        }

        return string.Empty;
    }
}
