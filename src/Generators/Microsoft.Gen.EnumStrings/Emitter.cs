// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.Gen.EnumStrings.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.EnumStrings;

#pragma warning disable LA0002 // Use 'Microsoft.Extensions.Text.NumericExtensions.ToInvariantString' for improved performance
#pragma warning disable S109 // Magic numbers should not be used

// Stryker disable all

/// <summary>
/// Emits fast ToInvariantString extension methods for enums.
/// </summary>
/// <remarks>
/// The generated code uses different strategies depending on the shape of the enum, and depending on what
/// symbols are available at compile time.
///
/// * If an enum has 1 or 2 entries, the lookup is done with explicit "if" statements.
///
/// * If an enum has mostly contiguous values (a common case), then the lookup is done via an array
///
/// * If an enum has a set of discontiguous values, then the lookup is done via a dictionary. This will be a frozen dictionary if the
///   frozen collections are available at compile time, otherwise a classic dictionary.
///
/// In all cases, if the initial lookup fails, then we lookup in a static concurrent dictionary as a cache of values from
/// the original Enum.ToString().
/// </remarks>
internal sealed class Emitter : EmitterBase
{
    // max # entries we keep in the concurrent dictionary
    private const int MaxCacheEntries = 256;

    // flags and sparse arrays can grow to this size no questions asked
    private const int ArrayLookupThreshold = 1024;

    // sparse arrays can get bigger than the threshold only if they don't exceed this percentage of sparseness.
    private const int MaxSparsePercent = 25;

    public string Emit(
        IEnumerable<ToStringMethod> toStringMethods,
        bool frozenDictionaryAvailable,
        CancellationToken cancellationToken)
    {
        foreach (var tsm in toStringMethods.OrderBy(static t => t.ExtensionNamespace + "." + t.ExtensionClass))
        {
            cancellationToken.ThrowIfCancellationRequested();
            GenExtension(tsm, frozenDictionaryAvailable);
        }

        return Capture();
    }

    // This code was stolen from .NET 6's implementation of Enum.ToString, and adapted for the circumstances
    private static string FlagsName(List<string> names, List<ulong> enumMemberValues, ulong valueToFormat)
    {
        ulong originalValueToFormat = valueToFormat;

        // Values are sorted, so if the incoming value is 0, we can check to see whether
        // the first entry matches it, in which case we can return its name; otherwise,
        // we can just return "0".
        if (valueToFormat == 0)
        {
            return enumMemberValues.Count > 0 && enumMemberValues[0] == 0 ? names[0] : "0";
        }

        // With a ulong result value, regardless of the enum's base type, the maximum
        // possible number of consistent name/values we could have is 64, since every
        // value is made up of one or more bits, and when we see values and incorporate
        // their names, we effectively switch off those bits.
        Span<int> foundItems = stackalloc int[64];

        // Walk from largest to smallest. It's common to have a flags enum with a single
        // value that matches a single entry, in which case we can just return the existing
        // name string.
        int index = enumMemberValues.Count - 1;
        while (index >= 0)
        {
            if (enumMemberValues[index] == valueToFormat)
            {
                return names[index];
            }

            if (enumMemberValues[index] < valueToFormat)
            {
                break;
            }

            index--;
        }

        // Now look for multiple matches, storing the indices of the values
        // into our span.
        int resultLength = 0;
        int foundItemsCount = 0;
        while (index >= 0)
        {
            ulong currentValue = enumMemberValues[index];
            if (index == 0 && currentValue == 0)
            {
                break;
            }

            if ((valueToFormat & currentValue) == currentValue)
            {
                valueToFormat -= currentValue;
                foundItems[foundItemsCount++] = index;
                resultLength += names[index].Length;
            }

            index--;
        }

        // If we exhausted looking through all the values and we still have
        // a non-zero result, we couldn't match the result to only named values.
        // In that case, we return null and let the call site just generate
        // a string for the integral value.
        if (valueToFormat != 0)
        {
            return originalValueToFormat.ToString(CultureInfo.InvariantCulture);
        }

        // We know what strings to concatenate.  Do so.

        const int SeparatorStringLength = 2; // ", "
        resultLength += SeparatorStringLength * (foundItemsCount - 1);
        char[] result = new char[resultLength];

        Span<char> resultSpan = result.AsSpan();
        string name = names[foundItems[--foundItemsCount]];
        for (int i = 0; i < name.Length; i++)
        {
            resultSpan[i] = name[i];
        }

        resultSpan = resultSpan.Slice(name.Length);
        while (--foundItemsCount >= 0)
        {
            resultSpan[0] = ',';
            resultSpan[1] = ' ';
            resultSpan = resultSpan.Slice(SeparatorStringLength);

            name = names[foundItems[foundItemsCount]];
            for (int i = 0; i < name.Length; i++)
            {
                resultSpan[i] = name[i];
            }

            resultSpan = resultSpan.Slice(name.Length);
        }

        return new string(result);
    }

    private static bool IsBigEnum(ToStringMethod tsm) => tsm.UnderlyingType is "ulong" or "long";

    private void GenExtension(ToStringMethod tsm, bool frozenDictionaryAvailable)
    {
        if (tsm.ExtensionNamespace.Length > 0)
        {
            OutLn($"namespace {tsm.ExtensionNamespace}");
            OutOpenBrace();
        }

        OutLn();
        OutLn($"/// <summary>");
        OutLn($"/// Extension methods for the <see cref=\"{tsm.EnumTypeName}\"/> enum.");
        OutLn($"/// </summary>");
        OutLn($"{tsm.ExtensionClassModifiers} class {tsm.ExtensionClass}");
        OutOpenBrace();

        var names = tsm.MemberNames;
        var values = tsm.MemberValues;
        var lookupType = PickLookupType(tsm, out var flagRange, values);
        var fieldPrefix = "__" + tsm.ExtensionMethod + "_";

        GenFields();
        GenMethod();

        OutCloseBrace();

        if (tsm.ExtensionNamespace.Length > 0)
        {
            OutCloseBrace();
        }

        static LookupType PickLookupType(ToStringMethod tsm, out ulong flagRange, List<ulong> values)
        {
            flagRange = 0;
            var lookupType = LookupType.Nothing;

            if (tsm.FlagsEnum)
            {
                foreach (var v in values)
                {
                    flagRange |= v;
                }

                if (values.Count == 1)
                {
                    lookupType = LookupType.Conditionals;
                }
                else if (flagRange < ArrayLookupThreshold)
                {
                    lookupType = LookupType.Array;
                }
                else
                {
                    lookupType = LookupType.Dictionary;
                }
            }
            else
            {
                if (values.Count < 3)
                {
                    lookupType = LookupType.Conditionals;
                }
                else
                {
                    var delta = values[values.Count - 1] - values[0] + 1;
                    if (delta == (ulong)values.Count)
                    {
                        lookupType = LookupType.Array;
                    }
                    else if (values[values.Count - 1] < ArrayLookupThreshold)
                    {
                        lookupType = LookupType.Array;
                    }
                    else
                    {
                        lookupType = LookupType.Array;

                        var numEmptySlots = delta - (ulong)values.Count;
                        var percenEmptySlots = (numEmptySlots * 100) / (ulong)values.Count;
                        if (percenEmptySlots > MaxSparsePercent)
                        {
                            lookupType = LookupType.Dictionary;
                        }
                    }
                }
            }

            return lookupType;
        }

        void GenMethod()
        {
            OutLn($"/// <summary>");
            OutLn($"/// Efficiently returns a string representation for a value of the <see cref=\"{tsm.EnumTypeName}\"/> enum.");
            OutLn($"/// </summary>");
            OutLn($"/// <param name=\"value\">The value to use.</param>");
            OutLn($"/// <returns>A string representation of the value, equivalent to what ToString would return.</returns>");
            OutLn($"/// <remarks>This function is equivalent to calling ToString on an enum's value, except that it is considerably faster.</remarks>");
            OutGeneratedCodeAttribute();
            OutLn($"public static string {tsm.ExtensionMethod}(this {tsm.EnumTypeName} value)");
            OutOpenBrace();

            var valueType = IsBigEnum(tsm) ? "ulong" : "uint";
            var valueText = IsBigEnum(tsm) ? "v" : "(int)v";

            OutLn($"var v = ({valueType})value;");

            switch (lookupType)
            {
                case LookupType.Conditionals:
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        var e = (i > 0) ? "else " : string.Empty;
                        OutLn($"{e}if (v == {GetLiteral(values[i])})");
                        OutOpenBrace();
                        OutLn($"return \"{names[i]}\";");
                        OutCloseBrace();
                    }

                    break;
                }

                case LookupType.Array:
                {
                    if (tsm.FlagsEnum)
                    {
                        OutLn($"if (v <= {flagRange})");
                        OutOpenBrace();
                        OutLn($"return {fieldPrefix}LookupArray[v];");
                        OutCloseBrace();
                    }
                    else
                    {
                        var upper = GetLiteral(values[values.Count - 1]);
                        if (values[0] > 0)
                        {
                            var lower = GetLiteral(values[0]);
                            OutLn($"if (v >= {lower} && v <= {upper})");
                            OutOpenBrace();
                            OutLn($"return {fieldPrefix}LookupArray[v - {lower}];");
                        }
                        else
                        {
                            if (IsBigEnum(tsm))
                            {
                                OutLn($"if (v <= {upper})");
                            }
                            else
                            {
                                OutLn($"if (v < {fieldPrefix}LookupArray.Length)");
                            }

                            OutOpenBrace();
                            OutLn($"return {fieldPrefix}LookupArray[v];");
                        }

                        OutCloseBrace();
                    }

                    break;
                }

                case LookupType.Dictionary:
                {
                    OutLn($"if ({fieldPrefix}LookupDictionary.TryGetValue({valueText}, out var lookupResult))");
                    OutOpenBrace();
                    OutLn($"return lookupResult;");
                    OutCloseBrace();
                    break;
                }
            }

            OutLn();
            OutLn($"{fieldPrefix}CacheDictionary ??= new();");
            OutLn($"if ({fieldPrefix}CacheDictionary.TryGetValue({valueText}, out var cachedResult))");
            OutOpenBrace();
            OutLn("return cachedResult;");
            OutCloseBrace();

            OutLn();
            OutLn($"var result = value.ToString();");

            OutLn();
            OutLn($"if ({fieldPrefix}ApproximateCacheCount < {MaxCacheEntries})");
            OutOpenBrace();
            OutLn($"_ = global::System.Threading.Interlocked.Increment(ref {fieldPrefix}ApproximateCacheCount);");
            OutLn($"{fieldPrefix}CacheDictionary[{valueText}] = result;");
            OutCloseBrace();

            OutLn();
            OutLn($"return result;");

            OutCloseBrace();

            string GetLiteral(ulong value) => IsBigEnum(tsm) ? value.ToString(CultureInfo.InvariantCulture) + "UL" : ((uint)value).ToString(CultureInfo.InvariantCulture) + "U";
        }

        void GenFields()
        {
            if (lookupType == LookupType.Array)
            {
                OutGeneratedCodeAttribute();
                OutLn($"private static readonly string[] {fieldPrefix}LookupArray = new string[]");
                OutOpenBrace();

                if (tsm.FlagsEnum)
                {
                    for (ulong i = 0; i <= flagRange; i++)
                    {
                        OutLn($"\"{FlagsName(names, values, i)}\",");
                    }
                }
                else
                {
                    OutLn($"\"{names[0]}\",");

                    ulong previous = values[0];
                    for (int i = 1; i < values.Count; i++)
                    {
                        while (previous < values[i] - 1)
                        {
                            previous++;
                            OutLn($"\"{previous.ToString(CultureInfo.InvariantCulture)}\",");
                        }

                        OutLn($"\"{names[i]}\",");
                        previous = values[i];
                    }
                }

                OutCloseBraceWithExtra(";");
            }
            else if (lookupType == LookupType.Dictionary)
            {
                OutGeneratedCodeAttribute();

                var isBigEnum = IsBigEnum(tsm);

#pragma warning disable S3358 // Ternary operators should not be nested
#pragma warning disable S103 // Lines should not be too long
                var decl = frozenDictionaryAvailable
                    ? isBigEnum
                        ? $"private static readonly global::System.Collections.Frozen.FrozenDictionary<ulong, string> {fieldPrefix}LookupDictionary = global::System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(new global::System.Collections.Generic.Dictionary<ulong, string>({values.Count})"
                        : $"private static readonly global::System.Collections.Frozen.FrozenDictionary<int, string> {fieldPrefix}LookupDictionary = global::System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(new global::System.Collections.Generic.Dictionary<int, string>({values.Count})"
                    : isBigEnum
                        ? $"private static readonly global::System.Collections.Generic.Dictionary<ulong, string> {fieldPrefix}LookupDictionary = new({values.Count})"
                        : $"private static readonly global::System.Collections.Generic.Dictionary<int, string> {fieldPrefix}LookupDictionary = new({values.Count})";
#pragma warning restore S103 // Lines should not be too long
#pragma warning restore S3358 // Ternary operators should not be nested

                OutLn(decl);
                OutOpenBrace();

                if (tsm.FlagsEnum)
                {
                    for (int i = 0; i < ArrayLookupThreshold; i++)
                    {
                        OutLn($"{{ {i.ToString(CultureInfo.InvariantCulture)}, \"{FlagsName(names, values, (ulong)i)}\" }},");
                    }

                    for (int i = 0; i < values.Count; i++)
                    {
                        if (values[i] >= ArrayLookupThreshold)
                        {
                            if (isBigEnum)
                            {
                                OutLn($"{{ {values[i].ToString(CultureInfo.InvariantCulture)}, \"{names[i]}\" }},");
                            }
                            else
                            {
                                OutLn($"{{ unchecked((int){(values[i] & 0xffffffff).ToString(CultureInfo.InvariantCulture)}), \"{names[i]}\" }},");
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        if (isBigEnum)
                        {
                            OutLn($"{{ {values[i].ToString(CultureInfo.InvariantCulture)}, \"{names[i]}\" }},");
                        }
                        else
                        {
                            OutLn($"{{ unchecked((int){(values[i] & 0xffffffff).ToString(CultureInfo.InvariantCulture)}), \"{names[i]}\" }},");
                        }
                    }
                }

                OutCloseBraceWithExtra(frozenDictionaryAvailable ? ");" : ";");
            }

            var keyType = IsBigEnum(tsm) ? "ulong" : "int";

            OutLn();
            OutGeneratedCodeAttribute();
            OutLn($"private static global::System.Collections.Concurrent.ConcurrentDictionary<{keyType}, string>? {fieldPrefix}CacheDictionary;");
            OutLn($"private static volatile int {fieldPrefix}ApproximateCacheCount;");
            OutLn();
        }
    }

    private enum LookupType
    {
        Nothing,
        Conditionals,
        Array,
        Dictionary,
    }
}
