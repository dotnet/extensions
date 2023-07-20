// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Gen.ContextualOptions.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.ContextualOptions;

internal sealed class Emitter : EmitterBase
{
    public string Emit(IEnumerable<OptionsContextType> list)
    {
        foreach (var optionsContextType in list)
        {
            OutLn(FormatClass(optionsContextType).ToString(CultureInfo.InvariantCulture));
        }

        return Capture();
    }

    [SuppressMessage(
        "StyleCop.CSharp.LayoutRules",
        "SA1513:Closing brace should be followed by blank line",
        Justification = "The spacing here is done intentionally to better reflect what the generated code will look like.")]
    private static FormattableString FormatClass(OptionsContextType optionsContextType) =>
    $@"{(!string.IsNullOrEmpty(optionsContextType.Namespace) ? $"namespace {optionsContextType.Namespace}" +
    "{" : string.Empty)}
    [{GeneratorUtilities.GeneratedCodeAttribute}]
    partial {optionsContextType.Keyword} {optionsContextType.Name} : global::Microsoft.Extensions.Options.Contextual.IOptionsContext
    {{
        [{GeneratorUtilities.GeneratedCodeAttribute}]
        void global::Microsoft.Extensions.Options.Contextual.IOptionsContext.PopulateReceiver<T>(T receiver)
        {{{string.Concat(optionsContextType.OptionsContextProperties.OrderBy(x => x).Select(property => $@"
            receiver.Receive(nameof({property}), {property});"))}
        }}
    }}
{(!string.IsNullOrEmpty(optionsContextType.Namespace) ? "}" : string.Empty)}";
}
