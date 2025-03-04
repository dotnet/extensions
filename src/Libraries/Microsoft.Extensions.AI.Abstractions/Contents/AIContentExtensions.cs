// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
#if NET
using System.Runtime.CompilerServices;
#endif

namespace Microsoft.Extensions.AI;

/// <summary>Internal extensions for working with <see cref="AIContent"/>.</summary>
internal static class AIContentExtensions
{
    /// <summary>Concatenates the text of all <see cref="TextContent"/> instances in the list.</summary>
    public static string ConcatText(this IEnumerable<AIContent> contents)
    {
        if (contents is IList<AIContent> list)
        {
            int count = list.Count;
            switch (count)
            {
                case 0:
                    return string.Empty;

                case 1:
                    return (list[0] as TextContent)?.Text ?? string.Empty;

#if NET
                default:
                    DefaultInterpolatedStringHandler builder = new(0, 0, null, stackalloc char[512]);
                    for (int i = 0; i < count; i++)
                    {
                        if (list[i] is TextContent text)
                        {
                            builder.AppendLiteral(text.Text);
                        }
                    }

                    return builder.ToStringAndClear();
#endif
            }
        }

        return string.Concat(contents.OfType<TextContent>());
    }
}
