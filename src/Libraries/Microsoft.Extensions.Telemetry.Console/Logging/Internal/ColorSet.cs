// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;
using Microsoft.Extensions.EnumStrings;

[assembly: EnumStrings(typeof(ConsoleColor))]

namespace Microsoft.Extensions.Telemetry.Console.Internal;

// An internal class from https://github.com/dotnet/runtime/blob/57e1c232ee4ce5a5a4413de4fc66544e4e346a62/src/libraries/Microsoft.Extensions.Logging.Console/src/SimpleConsoleFormatter.cs#L205
internal readonly struct ColorSet : IEquatable<ColorSet>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColorSet"/> struct.
    /// </summary>
    /// <param name="foreground">Foreground color.</param>
    /// <param name="background">Background color.</param>
    public ColorSet(ConsoleColor? foreground, ConsoleColor? background)
    {
        Foreground = foreground;
        Background = background;
    }

    internal ColorSet(ConsoleColor? background)
    {
        Foreground = null;
        Background = background;
    }

    /// <summary>
    /// Gets foreground color.
    /// </summary>
    public ConsoleColor? Foreground { get; }

    /// <summary>
    /// Gets background color.
    /// </summary>
    public ConsoleColor? Background { get; }

    /// <summary>
    /// Compares two colors.
    /// </summary>
    /// <param name="obj">Other color.</param>
    /// <returns><see langword="true"/> if equal.</returns>
    public override bool Equals(object? obj)
    {
        return obj is ColorSet set && Equals(set);
    }

    /// <summary>
    /// Compares two colors.
    /// </summary>
    /// <param name="other">Other color.</param>
    /// <returns><see langword="true"/> if equal.</returns>
    public bool Equals(ColorSet other)
    {
        return Foreground == other.Foreground &&
               Background == other.Background;
    }

    /// <summary>
    /// Get a unique hashcode for the color.
    /// </summary>
    /// <returns>Hash code.</returns>
    public override int GetHashCode() => HashCode.Combine(Foreground, Background);

    /// <summary>
    /// Compares two color sets for equality.
    /// </summary>
    /// <param name="left">Left color set.</param>
    /// <param name="right">Right color set.</param>
    /// <returns><see langword="true"/> if equal.</returns>
    public static bool operator ==(ColorSet left, ColorSet right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two color sets for inequality.
    /// </summary>
    /// <param name="left">Left color set.</param>
    /// <param name="right">Right color set.</param>
    /// <returns><see langword="true"/> if not equal.</returns>
    public static bool operator !=(ColorSet left, ColorSet right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Returns a string representation of this instance.
    /// </summary>
    /// <returns>A <see cref="string"/> that represents current color set.</returns>
    public override string ToString()
    {
        var foreground = Foreground?.ToInvariantString() ?? "None";
        var background = Background?.ToInvariantString() ?? "None";
        return foreground + " on " + background;
    }
}
#endif
