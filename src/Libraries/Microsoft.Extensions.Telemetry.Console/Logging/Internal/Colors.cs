// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;

namespace Microsoft.Extensions.Telemetry.Console.Internal;

/// <summary>
/// The set of predefined values of <see cref="ColorSet"/> type.
/// </summary>
internal static class Colors
{
    public static readonly ColorSet None = new(null, null);

    public static readonly ColorSet BlackOnDarkBlue =
        new(ConsoleColor.Black, ConsoleColor.DarkBlue);

    public static readonly ColorSet BlackOnDarkGreen =
        new(ConsoleColor.Black, ConsoleColor.DarkGreen);

    public static readonly ColorSet BlackOnDarkCyan =
        new(ConsoleColor.Black, ConsoleColor.DarkCyan);

    public static readonly ColorSet BlackOnDarkRed =
        new(ConsoleColor.Black, ConsoleColor.DarkRed);

    public static readonly ColorSet BlackOnDarkMagenta =
        new(ConsoleColor.Black, ConsoleColor.DarkMagenta);

    public static readonly ColorSet BlackOnDarkYellow =
        new(ConsoleColor.Black, ConsoleColor.DarkYellow);

    public static readonly ColorSet BlackOnGray =
        new(ConsoleColor.Black, ConsoleColor.Gray);

    public static readonly ColorSet BlackOnDarkGray =
        new(ConsoleColor.Black, ConsoleColor.DarkGray);

    public static readonly ColorSet BlackOnBlue =
        new(ConsoleColor.Black, ConsoleColor.Blue);

    public static readonly ColorSet BlackOnGreen =
        new(ConsoleColor.Black, ConsoleColor.Green);

    public static readonly ColorSet BlackOnCyan =
        new(ConsoleColor.Black, ConsoleColor.Cyan);

    public static readonly ColorSet BlackOnRed =
        new(ConsoleColor.Black, ConsoleColor.Red);

    public static readonly ColorSet BlackOnMagenta =
        new(ConsoleColor.Black, ConsoleColor.Magenta);

    public static readonly ColorSet BlackOnYellow =
        new(ConsoleColor.Black, ConsoleColor.Yellow);

    public static readonly ColorSet BlackOnWhite =
        new(ConsoleColor.Black, ConsoleColor.White);

    public static readonly ColorSet BlackOnNone =
        new(ConsoleColor.Black, null);

    public static readonly ColorSet DarkBlueOnBlack =
        new(ConsoleColor.DarkBlue, ConsoleColor.Black);

    public static readonly ColorSet DarkBlueOnDarkGreen =
        new(ConsoleColor.DarkBlue, ConsoleColor.DarkGreen);

    public static readonly ColorSet DarkBlueOnDarkCyan =
        new(ConsoleColor.DarkBlue, ConsoleColor.DarkCyan);

    public static readonly ColorSet DarkBlueOnDarkRed =
        new(ConsoleColor.DarkBlue, ConsoleColor.DarkRed);

    public static readonly ColorSet DarkBlueOnDarkMagenta =
        new(ConsoleColor.DarkBlue, ConsoleColor.DarkMagenta);

    public static readonly ColorSet DarkBlueOnDarkYellow =
        new(ConsoleColor.DarkBlue, ConsoleColor.DarkYellow);

    public static readonly ColorSet DarkBlueOnGray =
        new(ConsoleColor.DarkBlue, ConsoleColor.Gray);

    public static readonly ColorSet DarkBlueOnDarkGray =
        new(ConsoleColor.DarkBlue, ConsoleColor.DarkGray);

    public static readonly ColorSet DarkBlueOnBlue =
        new(ConsoleColor.DarkBlue, ConsoleColor.Blue);

    public static readonly ColorSet DarkBlueOnGreen =
        new(ConsoleColor.DarkBlue, ConsoleColor.Green);

    public static readonly ColorSet DarkBlueOnCyan =
        new(ConsoleColor.DarkBlue, ConsoleColor.Cyan);

    public static readonly ColorSet DarkBlueOnRed =
        new(ConsoleColor.DarkBlue, ConsoleColor.Red);

    public static readonly ColorSet DarkBlueOnMagenta =
        new(ConsoleColor.DarkBlue, ConsoleColor.Magenta);

    public static readonly ColorSet DarkBlueOnYellow =
        new(ConsoleColor.DarkBlue, ConsoleColor.Yellow);

    public static readonly ColorSet DarkBlueOnWhite =
        new(ConsoleColor.DarkBlue, ConsoleColor.White);

    public static readonly ColorSet DarkBlueOnNone =
        new(ConsoleColor.DarkBlue, null);

    public static readonly ColorSet DarkGreenOnBlack =
        new(ConsoleColor.DarkGreen, ConsoleColor.Black);

    public static readonly ColorSet DarkGreenOnDarkBlue =
        new(ConsoleColor.DarkGreen, ConsoleColor.DarkBlue);

    public static readonly ColorSet DarkGreenOnDarkCyan =
        new(ConsoleColor.DarkGreen, ConsoleColor.DarkCyan);

    public static readonly ColorSet DarkGreenOnDarkRed =
        new(ConsoleColor.DarkGreen, ConsoleColor.DarkRed);

    public static readonly ColorSet DarkGreenOnDarkMagenta =
        new(ConsoleColor.DarkGreen, ConsoleColor.DarkMagenta);

    public static readonly ColorSet DarkGreenOnDarkYellow =
        new(ConsoleColor.DarkGreen, ConsoleColor.DarkYellow);

    public static readonly ColorSet DarkGreenOnGray =
        new(ConsoleColor.DarkGreen, ConsoleColor.Gray);

    public static readonly ColorSet DarkGreenOnDarkGray =
        new(ConsoleColor.DarkGreen, ConsoleColor.DarkGray);

    public static readonly ColorSet DarkGreenOnBlue =
        new(ConsoleColor.DarkGreen, ConsoleColor.Blue);

    public static readonly ColorSet DarkGreenOnGreen =
        new(ConsoleColor.DarkGreen, ConsoleColor.Green);

    public static readonly ColorSet DarkGreenOnCyan =
        new(ConsoleColor.DarkGreen, ConsoleColor.Cyan);

    public static readonly ColorSet DarkGreenOnRed =
        new(ConsoleColor.DarkGreen, ConsoleColor.Red);

    public static readonly ColorSet DarkGreenOnMagenta =
        new(ConsoleColor.DarkGreen, ConsoleColor.Magenta);

    public static readonly ColorSet DarkGreenOnYellow =
        new(ConsoleColor.DarkGreen, ConsoleColor.Yellow);

    public static readonly ColorSet DarkGreenOnWhite =
        new(ConsoleColor.DarkGreen, ConsoleColor.White);

    public static readonly ColorSet DarkGreenOnNone =
        new(ConsoleColor.DarkGreen, null);

    public static readonly ColorSet DarkCyanOnBlack =
        new(ConsoleColor.DarkCyan, ConsoleColor.Black);

    public static readonly ColorSet DarkCyanOnDarkBlue =
        new(ConsoleColor.DarkCyan, ConsoleColor.DarkBlue);

    public static readonly ColorSet DarkCyanOnDarkGreen =
        new(ConsoleColor.DarkCyan, ConsoleColor.DarkGreen);

    public static readonly ColorSet DarkCyanOnDarkRed =
        new(ConsoleColor.DarkCyan, ConsoleColor.DarkRed);

    public static readonly ColorSet DarkCyanOnDarkMagenta =
        new(ConsoleColor.DarkCyan, ConsoleColor.DarkMagenta);

    public static readonly ColorSet DarkCyanOnDarkYellow =
        new(ConsoleColor.DarkCyan, ConsoleColor.DarkYellow);

    public static readonly ColorSet DarkCyanOnGray =
        new(ConsoleColor.DarkCyan, ConsoleColor.Gray);

    public static readonly ColorSet DarkCyanOnDarkGray =
        new(ConsoleColor.DarkCyan, ConsoleColor.DarkGray);

    public static readonly ColorSet DarkCyanOnBlue =
        new(ConsoleColor.DarkCyan, ConsoleColor.Blue);

    public static readonly ColorSet DarkCyanOnGreen =
        new(ConsoleColor.DarkCyan, ConsoleColor.Green);

    public static readonly ColorSet DarkCyanOnCyan =
        new(ConsoleColor.DarkCyan, ConsoleColor.Cyan);

    public static readonly ColorSet DarkCyanOnRed =
        new(ConsoleColor.DarkCyan, ConsoleColor.Red);

    public static readonly ColorSet DarkCyanOnMagenta =
        new(ConsoleColor.DarkCyan, ConsoleColor.Magenta);

    public static readonly ColorSet DarkCyanOnYellow =
        new(ConsoleColor.DarkCyan, ConsoleColor.Yellow);

    public static readonly ColorSet DarkCyanOnWhite =
        new(ConsoleColor.DarkCyan, ConsoleColor.White);

    public static readonly ColorSet DarkCyanOnNone =
        new(ConsoleColor.DarkCyan, null);

    public static readonly ColorSet DarkRedOnBlack =
        new(ConsoleColor.DarkRed, ConsoleColor.Black);

    public static readonly ColorSet DarkRedOnDarkBlue =
        new(ConsoleColor.DarkRed, ConsoleColor.DarkBlue);

    public static readonly ColorSet DarkRedOnDarkGreen =
        new(ConsoleColor.DarkRed, ConsoleColor.DarkGreen);

    public static readonly ColorSet DarkRedOnDarkCyan =
        new(ConsoleColor.DarkRed, ConsoleColor.DarkCyan);

    public static readonly ColorSet DarkRedOnDarkMagenta =
        new(ConsoleColor.DarkRed, ConsoleColor.DarkMagenta);

    public static readonly ColorSet DarkRedOnDarkYellow =
        new(ConsoleColor.DarkRed, ConsoleColor.DarkYellow);

    public static readonly ColorSet DarkRedOnGray =
        new(ConsoleColor.DarkRed, ConsoleColor.Gray);

    public static readonly ColorSet DarkRedOnDarkGray =
        new(ConsoleColor.DarkRed, ConsoleColor.DarkGray);

    public static readonly ColorSet DarkRedOnBlue =
        new(ConsoleColor.DarkRed, ConsoleColor.Blue);

    public static readonly ColorSet DarkRedOnGreen =
        new(ConsoleColor.DarkRed, ConsoleColor.Green);

    public static readonly ColorSet DarkRedOnCyan =
        new(ConsoleColor.DarkRed, ConsoleColor.Cyan);

    public static readonly ColorSet DarkRedOnRed =
        new(ConsoleColor.DarkRed, ConsoleColor.Red);

    public static readonly ColorSet DarkRedOnMagenta =
        new(ConsoleColor.DarkRed, ConsoleColor.Magenta);

    public static readonly ColorSet DarkRedOnYellow =
        new(ConsoleColor.DarkRed, ConsoleColor.Yellow);

    public static readonly ColorSet DarkRedOnWhite =
        new(ConsoleColor.DarkRed, ConsoleColor.White);

    public static readonly ColorSet DarkRedOnNone =
        new(ConsoleColor.DarkRed, null);

    public static readonly ColorSet DarkMagentaOnBlack =
        new(ConsoleColor.DarkMagenta, ConsoleColor.Black);

    public static readonly ColorSet DarkMagentaOnDarkBlue =
        new(ConsoleColor.DarkMagenta, ConsoleColor.DarkBlue);

    public static readonly ColorSet DarkMagentaOnDarkGreen =
        new(ConsoleColor.DarkMagenta, ConsoleColor.DarkGreen);

    public static readonly ColorSet DarkMagentaOnDarkCyan =
        new(ConsoleColor.DarkMagenta, ConsoleColor.DarkCyan);

    public static readonly ColorSet DarkMagentaOnDarkRed =
        new(ConsoleColor.DarkMagenta, ConsoleColor.DarkRed);

    public static readonly ColorSet DarkMagentaOnDarkYellow =
        new(ConsoleColor.DarkMagenta, ConsoleColor.DarkYellow);

    public static readonly ColorSet DarkMagentaOnGray =
        new(ConsoleColor.DarkMagenta, ConsoleColor.Gray);

    public static readonly ColorSet DarkMagentaOnDarkGray =
        new(ConsoleColor.DarkMagenta, ConsoleColor.DarkGray);

    public static readonly ColorSet DarkMagentaOnBlue =
        new(ConsoleColor.DarkMagenta, ConsoleColor.Blue);

    public static readonly ColorSet DarkMagentaOnGreen =
        new(ConsoleColor.DarkMagenta, ConsoleColor.Green);

    public static readonly ColorSet DarkMagentaOnCyan =
        new(ConsoleColor.DarkMagenta, ConsoleColor.Cyan);

    public static readonly ColorSet DarkMagentaOnRed =
        new(ConsoleColor.DarkMagenta, ConsoleColor.Red);

    public static readonly ColorSet DarkMagentaOnMagenta =
        new(ConsoleColor.DarkMagenta, ConsoleColor.Magenta);

    public static readonly ColorSet DarkMagentaOnYellow =
        new(ConsoleColor.DarkMagenta, ConsoleColor.Yellow);

    public static readonly ColorSet DarkMagentaOnWhite =
        new(ConsoleColor.DarkMagenta, ConsoleColor.White);

    public static readonly ColorSet DarkMagentaOnNone =
        new(ConsoleColor.DarkMagenta, null);

    public static readonly ColorSet DarkYellowOnBlack =
        new(ConsoleColor.DarkYellow, ConsoleColor.Black);

    public static readonly ColorSet DarkYellowOnDarkBlue =
        new(ConsoleColor.DarkYellow, ConsoleColor.DarkBlue);

    public static readonly ColorSet DarkYellowOnDarkGreen =
        new(ConsoleColor.DarkYellow, ConsoleColor.DarkGreen);

    public static readonly ColorSet DarkYellowOnDarkCyan =
        new(ConsoleColor.DarkYellow, ConsoleColor.DarkCyan);

    public static readonly ColorSet DarkYellowOnDarkRed =
        new(ConsoleColor.DarkYellow, ConsoleColor.DarkRed);

    public static readonly ColorSet DarkYellowOnDarkMagenta =
        new(ConsoleColor.DarkYellow, ConsoleColor.DarkMagenta);

    public static readonly ColorSet DarkYellowOnGray =
        new(ConsoleColor.DarkYellow, ConsoleColor.Gray);

    public static readonly ColorSet DarkYellowOnDarkGray =
        new(ConsoleColor.DarkYellow, ConsoleColor.DarkGray);

    public static readonly ColorSet DarkYellowOnBlue =
        new(ConsoleColor.DarkYellow, ConsoleColor.Blue);

    public static readonly ColorSet DarkYellowOnGreen =
        new(ConsoleColor.DarkYellow, ConsoleColor.Green);

    public static readonly ColorSet DarkYellowOnCyan =
        new(ConsoleColor.DarkYellow, ConsoleColor.Cyan);

    public static readonly ColorSet DarkYellowOnRed =
        new(ConsoleColor.DarkYellow, ConsoleColor.Red);

    public static readonly ColorSet DarkYellowOnMagenta =
        new(ConsoleColor.DarkYellow, ConsoleColor.Magenta);

    public static readonly ColorSet DarkYellowOnYellow =
        new(ConsoleColor.DarkYellow, ConsoleColor.Yellow);

    public static readonly ColorSet DarkYellowOnWhite =
        new(ConsoleColor.DarkYellow, ConsoleColor.White);

    public static readonly ColorSet DarkYellowOnNone =
        new(ConsoleColor.DarkYellow, null);

    public static readonly ColorSet GrayOnBlack =
        new(ConsoleColor.Gray, ConsoleColor.Black);

    public static readonly ColorSet GrayOnDarkBlue =
        new(ConsoleColor.Gray, ConsoleColor.DarkBlue);

    public static readonly ColorSet GrayOnDarkGreen =
        new(ConsoleColor.Gray, ConsoleColor.DarkGreen);

    public static readonly ColorSet GrayOnDarkCyan =
        new(ConsoleColor.Gray, ConsoleColor.DarkCyan);

    public static readonly ColorSet GrayOnDarkRed =
        new(ConsoleColor.Gray, ConsoleColor.DarkRed);

    public static readonly ColorSet GrayOnDarkMagenta =
        new(ConsoleColor.Gray, ConsoleColor.DarkMagenta);

    public static readonly ColorSet GrayOnDarkYellow =
        new(ConsoleColor.Gray, ConsoleColor.DarkYellow);

    public static readonly ColorSet GrayOnDarkGray =
        new(ConsoleColor.Gray, ConsoleColor.DarkGray);

    public static readonly ColorSet GrayOnBlue =
        new(ConsoleColor.Gray, ConsoleColor.Blue);

    public static readonly ColorSet GrayOnGreen =
        new(ConsoleColor.Gray, ConsoleColor.Green);

    public static readonly ColorSet GrayOnCyan =
        new(ConsoleColor.Gray, ConsoleColor.Cyan);

    public static readonly ColorSet GrayOnRed =
        new(ConsoleColor.Gray, ConsoleColor.Red);

    public static readonly ColorSet GrayOnMagenta =
        new(ConsoleColor.Gray, ConsoleColor.Magenta);

    public static readonly ColorSet GrayOnYellow =
        new(ConsoleColor.Gray, ConsoleColor.Yellow);

    public static readonly ColorSet GrayOnWhite =
        new(ConsoleColor.Gray, ConsoleColor.White);

    public static readonly ColorSet GrayOnNone =
        new(ConsoleColor.Gray, null);

    public static readonly ColorSet DarkGrayOnBlack =
        new(ConsoleColor.DarkGray, ConsoleColor.Black);

    public static readonly ColorSet DarkGrayOnDarkBlue =
        new(ConsoleColor.DarkGray, ConsoleColor.DarkBlue);

    public static readonly ColorSet DarkGrayOnDarkGreen =
        new(ConsoleColor.DarkGray, ConsoleColor.DarkGreen);

    public static readonly ColorSet DarkGrayOnDarkCyan =
        new(ConsoleColor.DarkGray, ConsoleColor.DarkCyan);

    public static readonly ColorSet DarkGrayOnDarkRed =
        new(ConsoleColor.DarkGray, ConsoleColor.DarkRed);

    public static readonly ColorSet DarkGrayOnDarkMagenta =
        new(ConsoleColor.DarkGray, ConsoleColor.DarkMagenta);

    public static readonly ColorSet DarkGrayOnDarkYellow =
        new(ConsoleColor.DarkGray, ConsoleColor.DarkYellow);

    public static readonly ColorSet DarkGrayOnGray =
        new(ConsoleColor.DarkGray, ConsoleColor.Gray);

    public static readonly ColorSet DarkGrayOnBlue =
        new(ConsoleColor.DarkGray, ConsoleColor.Blue);

    public static readonly ColorSet DarkGrayOnGreen =
        new(ConsoleColor.DarkGray, ConsoleColor.Green);

    public static readonly ColorSet DarkGrayOnCyan =
        new(ConsoleColor.DarkGray, ConsoleColor.Cyan);

    public static readonly ColorSet DarkGrayOnRed =
        new(ConsoleColor.DarkGray, ConsoleColor.Red);

    public static readonly ColorSet DarkGrayOnMagenta =
        new(ConsoleColor.DarkGray, ConsoleColor.Magenta);

    public static readonly ColorSet DarkGrayOnYellow =
        new(ConsoleColor.DarkGray, ConsoleColor.Yellow);

    public static readonly ColorSet DarkGrayOnWhite =
        new(ConsoleColor.DarkGray, ConsoleColor.White);

    public static readonly ColorSet DarkGrayOnNone =
        new(ConsoleColor.DarkGray, null);

    public static readonly ColorSet BlueOnBlack =
        new(ConsoleColor.Blue, ConsoleColor.Black);

    public static readonly ColorSet BlueOnDarkBlue =
        new(ConsoleColor.Blue, ConsoleColor.DarkBlue);

    public static readonly ColorSet BlueOnDarkGreen =
        new(ConsoleColor.Blue, ConsoleColor.DarkGreen);

    public static readonly ColorSet BlueOnDarkCyan =
        new(ConsoleColor.Blue, ConsoleColor.DarkCyan);

    public static readonly ColorSet BlueOnDarkRed =
        new(ConsoleColor.Blue, ConsoleColor.DarkRed);

    public static readonly ColorSet BlueOnDarkMagenta =
        new(ConsoleColor.Blue, ConsoleColor.DarkMagenta);

    public static readonly ColorSet BlueOnDarkYellow =
        new(ConsoleColor.Blue, ConsoleColor.DarkYellow);

    public static readonly ColorSet BlueOnGray =
        new(ConsoleColor.Blue, ConsoleColor.Gray);

    public static readonly ColorSet BlueOnDarkGray =
        new(ConsoleColor.Blue, ConsoleColor.DarkGray);

    public static readonly ColorSet BlueOnGreen =
        new(ConsoleColor.Blue, ConsoleColor.Green);

    public static readonly ColorSet BlueOnCyan =
        new(ConsoleColor.Blue, ConsoleColor.Cyan);

    public static readonly ColorSet BlueOnRed =
        new(ConsoleColor.Blue, ConsoleColor.Red);

    public static readonly ColorSet BlueOnMagenta =
        new(ConsoleColor.Blue, ConsoleColor.Magenta);

    public static readonly ColorSet BlueOnYellow =
        new(ConsoleColor.Blue, ConsoleColor.Yellow);

    public static readonly ColorSet BlueOnWhite =
        new(ConsoleColor.Blue, ConsoleColor.White);

    public static readonly ColorSet BlueOnNone =
        new(ConsoleColor.Blue, null);

    public static readonly ColorSet GreenOnBlack =
        new(ConsoleColor.Green, ConsoleColor.Black);

    public static readonly ColorSet GreenOnDarkBlue =
        new(ConsoleColor.Green, ConsoleColor.DarkBlue);

    public static readonly ColorSet GreenOnDarkGreen =
        new(ConsoleColor.Green, ConsoleColor.DarkGreen);

    public static readonly ColorSet GreenOnDarkCyan =
        new(ConsoleColor.Green, ConsoleColor.DarkCyan);

    public static readonly ColorSet GreenOnDarkRed =
        new(ConsoleColor.Green, ConsoleColor.DarkRed);

    public static readonly ColorSet GreenOnDarkMagenta =
        new(ConsoleColor.Green, ConsoleColor.DarkMagenta);

    public static readonly ColorSet GreenOnDarkYellow =
        new(ConsoleColor.Green, ConsoleColor.DarkYellow);

    public static readonly ColorSet GreenOnGray =
        new(ConsoleColor.Green, ConsoleColor.Gray);

    public static readonly ColorSet GreenOnDarkGray =
        new(ConsoleColor.Green, ConsoleColor.DarkGray);

    public static readonly ColorSet GreenOnBlue =
        new(ConsoleColor.Green, ConsoleColor.Blue);

    public static readonly ColorSet GreenOnCyan =
        new(ConsoleColor.Green, ConsoleColor.Cyan);

    public static readonly ColorSet GreenOnRed =
        new(ConsoleColor.Green, ConsoleColor.Red);

    public static readonly ColorSet GreenOnMagenta =
        new(ConsoleColor.Green, ConsoleColor.Magenta);

    public static readonly ColorSet GreenOnYellow =
        new(ConsoleColor.Green, ConsoleColor.Yellow);

    public static readonly ColorSet GreenOnWhite =
        new(ConsoleColor.Green, ConsoleColor.White);

    public static readonly ColorSet GreenOnNone =
        new(ConsoleColor.Green, null);

    public static readonly ColorSet CyanOnBlack =
        new(ConsoleColor.Cyan, ConsoleColor.Black);

    public static readonly ColorSet CyanOnDarkBlue =
        new(ConsoleColor.Cyan, ConsoleColor.DarkBlue);

    public static readonly ColorSet CyanOnDarkGreen =
        new(ConsoleColor.Cyan, ConsoleColor.DarkGreen);

    public static readonly ColorSet CyanOnDarkCyan =
        new(ConsoleColor.Cyan, ConsoleColor.DarkCyan);

    public static readonly ColorSet CyanOnDarkRed =
        new(ConsoleColor.Cyan, ConsoleColor.DarkRed);

    public static readonly ColorSet CyanOnDarkMagenta =
        new(ConsoleColor.Cyan, ConsoleColor.DarkMagenta);

    public static readonly ColorSet CyanOnDarkYellow =
        new(ConsoleColor.Cyan, ConsoleColor.DarkYellow);

    public static readonly ColorSet CyanOnGray =
        new(ConsoleColor.Cyan, ConsoleColor.Gray);

    public static readonly ColorSet CyanOnDarkGray =
        new(ConsoleColor.Cyan, ConsoleColor.DarkGray);

    public static readonly ColorSet CyanOnBlue =
        new(ConsoleColor.Cyan, ConsoleColor.Blue);

    public static readonly ColorSet CyanOnGreen =
        new(ConsoleColor.Cyan, ConsoleColor.Green);

    public static readonly ColorSet CyanOnRed =
        new(ConsoleColor.Cyan, ConsoleColor.Red);

    public static readonly ColorSet CyanOnMagenta =
        new(ConsoleColor.Cyan, ConsoleColor.Magenta);

    public static readonly ColorSet CyanOnYellow =
        new(ConsoleColor.Cyan, ConsoleColor.Yellow);

    public static readonly ColorSet CyanOnWhite =
        new(ConsoleColor.Cyan, ConsoleColor.White);

    public static readonly ColorSet CyanOnNone =
        new(ConsoleColor.Cyan, null);

    public static readonly ColorSet RedOnBlack =
        new(ConsoleColor.Red, ConsoleColor.Black);

    public static readonly ColorSet RedOnDarkBlue =
        new(ConsoleColor.Red, ConsoleColor.DarkBlue);

    public static readonly ColorSet RedOnDarkGreen =
        new(ConsoleColor.Red, ConsoleColor.DarkGreen);

    public static readonly ColorSet RedOnDarkCyan =
        new(ConsoleColor.Red, ConsoleColor.DarkCyan);

    public static readonly ColorSet RedOnDarkRed =
        new(ConsoleColor.Red, ConsoleColor.DarkRed);

    public static readonly ColorSet RedOnDarkMagenta =
        new(ConsoleColor.Red, ConsoleColor.DarkMagenta);

    public static readonly ColorSet RedOnDarkYellow =
        new(ConsoleColor.Red, ConsoleColor.DarkYellow);

    public static readonly ColorSet RedOnGray =
        new(ConsoleColor.Red, ConsoleColor.Gray);

    public static readonly ColorSet RedOnDarkGray =
        new(ConsoleColor.Red, ConsoleColor.DarkGray);

    public static readonly ColorSet RedOnBlue =
        new(ConsoleColor.Red, ConsoleColor.Blue);

    public static readonly ColorSet RedOnGreen =
        new(ConsoleColor.Red, ConsoleColor.Green);

    public static readonly ColorSet RedOnCyan =
        new(ConsoleColor.Red, ConsoleColor.Cyan);

    public static readonly ColorSet RedOnMagenta =
        new(ConsoleColor.Red, ConsoleColor.Magenta);

    public static readonly ColorSet RedOnYellow =
        new(ConsoleColor.Red, ConsoleColor.Yellow);

    public static readonly ColorSet RedOnWhite =
        new(ConsoleColor.Red, ConsoleColor.White);

    public static readonly ColorSet RedOnNone =
        new(ConsoleColor.Red, null);

    public static readonly ColorSet MagentaOnBlack =
        new(ConsoleColor.Magenta, ConsoleColor.Black);

    public static readonly ColorSet MagentaOnDarkBlue =
        new(ConsoleColor.Magenta, ConsoleColor.DarkBlue);

    public static readonly ColorSet MagentaOnDarkGreen =
        new(ConsoleColor.Magenta, ConsoleColor.DarkGreen);

    public static readonly ColorSet MagentaOnDarkCyan =
        new(ConsoleColor.Magenta, ConsoleColor.DarkCyan);

    public static readonly ColorSet MagentaOnDarkRed =
        new(ConsoleColor.Magenta, ConsoleColor.DarkRed);

    public static readonly ColorSet MagentaOnDarkMagenta =
        new(ConsoleColor.Magenta, ConsoleColor.DarkMagenta);

    public static readonly ColorSet MagentaOnDarkYellow =
        new(ConsoleColor.Magenta, ConsoleColor.DarkYellow);

    public static readonly ColorSet MagentaOnGray =
        new(ConsoleColor.Magenta, ConsoleColor.Gray);

    public static readonly ColorSet MagentaOnDarkGray =
        new(ConsoleColor.Magenta, ConsoleColor.DarkGray);

    public static readonly ColorSet MagentaOnBlue =
        new(ConsoleColor.Magenta, ConsoleColor.Blue);

    public static readonly ColorSet MagentaOnGreen =
        new(ConsoleColor.Magenta, ConsoleColor.Green);

    public static readonly ColorSet MagentaOnCyan =
        new(ConsoleColor.Magenta, ConsoleColor.Cyan);

    public static readonly ColorSet MagentaOnRed =
        new(ConsoleColor.Magenta, ConsoleColor.Red);

    public static readonly ColorSet MagentaOnYellow =
        new(ConsoleColor.Magenta, ConsoleColor.Yellow);

    public static readonly ColorSet MagentaOnWhite =
        new(ConsoleColor.Magenta, ConsoleColor.White);

    public static readonly ColorSet MagentaOnNone =
        new(ConsoleColor.Magenta, null);

    public static readonly ColorSet YellowOnBlack =
        new(ConsoleColor.Yellow, ConsoleColor.Black);

    public static readonly ColorSet YellowOnDarkBlue =
        new(ConsoleColor.Yellow, ConsoleColor.DarkBlue);

    public static readonly ColorSet YellowOnDarkGreen =
        new(ConsoleColor.Yellow, ConsoleColor.DarkGreen);

    public static readonly ColorSet YellowOnDarkCyan =
        new(ConsoleColor.Yellow, ConsoleColor.DarkCyan);

    public static readonly ColorSet YellowOnDarkRed =
        new(ConsoleColor.Yellow, ConsoleColor.DarkRed);

    public static readonly ColorSet YellowOnDarkMagenta =
        new(ConsoleColor.Yellow, ConsoleColor.DarkMagenta);

    public static readonly ColorSet YellowOnDarkYellow =
        new(ConsoleColor.Yellow, ConsoleColor.DarkYellow);

    public static readonly ColorSet YellowOnGray =
        new(ConsoleColor.Yellow, ConsoleColor.Gray);

    public static readonly ColorSet YellowOnDarkGray =
        new(ConsoleColor.Yellow, ConsoleColor.DarkGray);

    public static readonly ColorSet YellowOnBlue =
        new(ConsoleColor.Yellow, ConsoleColor.Blue);

    public static readonly ColorSet YellowOnGreen =
        new(ConsoleColor.Yellow, ConsoleColor.Green);

    public static readonly ColorSet YellowOnCyan =
        new(ConsoleColor.Yellow, ConsoleColor.Cyan);

    public static readonly ColorSet YellowOnRed =
        new(ConsoleColor.Yellow, ConsoleColor.Red);

    public static readonly ColorSet YellowOnMagenta =
        new(ConsoleColor.Yellow, ConsoleColor.Magenta);

    public static readonly ColorSet YellowOnWhite =
        new(ConsoleColor.Yellow, ConsoleColor.White);

    public static readonly ColorSet YellowOnNone =
        new(ConsoleColor.Yellow, null);

    public static readonly ColorSet WhiteOnBlack =
        new(ConsoleColor.White, ConsoleColor.Black);

    public static readonly ColorSet WhiteOnDarkBlue =
        new(ConsoleColor.White, ConsoleColor.DarkBlue);

    public static readonly ColorSet WhiteOnDarkGreen =
        new(ConsoleColor.White, ConsoleColor.DarkGreen);

    public static readonly ColorSet WhiteOnDarkCyan =
        new(ConsoleColor.White, ConsoleColor.DarkCyan);

    public static readonly ColorSet WhiteOnDarkRed =
        new(ConsoleColor.White, ConsoleColor.DarkRed);

    public static readonly ColorSet WhiteOnDarkMagenta =
        new(ConsoleColor.White, ConsoleColor.DarkMagenta);

    public static readonly ColorSet WhiteOnDarkYellow =
        new(ConsoleColor.White, ConsoleColor.DarkYellow);

    public static readonly ColorSet WhiteOnGray =
        new(ConsoleColor.White, ConsoleColor.Gray);

    public static readonly ColorSet WhiteOnDarkGray =
        new(ConsoleColor.White, ConsoleColor.DarkGray);

    public static readonly ColorSet WhiteOnBlue =
        new(ConsoleColor.White, ConsoleColor.Blue);

    public static readonly ColorSet WhiteOnGreen =
        new(ConsoleColor.White, ConsoleColor.Green);

    public static readonly ColorSet WhiteOnCyan =
        new(ConsoleColor.White, ConsoleColor.Cyan);

    public static readonly ColorSet WhiteOnRed =
        new(ConsoleColor.White, ConsoleColor.Red);

    public static readonly ColorSet WhiteOnMagenta =
        new(ConsoleColor.White, ConsoleColor.Magenta);

    public static readonly ColorSet WhiteOnYellow =
        new(ConsoleColor.White, ConsoleColor.Yellow);

    public static readonly ColorSet WhiteOnNone =
        new(ConsoleColor.White, null);

    public static readonly ColorSet NoneOnBlack =
        new(ConsoleColor.Black);

    public static readonly ColorSet NoneOnDarkBlue =
        new(ConsoleColor.DarkBlue);

    public static readonly ColorSet NoneOnDarkGreen =
        new(ConsoleColor.DarkGreen);

    public static readonly ColorSet NoneOnDarkCyan =
        new(ConsoleColor.DarkCyan);

    public static readonly ColorSet NoneOnDarkRed =
        new(ConsoleColor.DarkRed);

    public static readonly ColorSet NoneOnDarkMagenta =
        new(ConsoleColor.DarkMagenta);

    public static readonly ColorSet NoneOnDarkYellow =
        new(ConsoleColor.DarkYellow);

    public static readonly ColorSet NoneOnGray =
        new(ConsoleColor.Gray);

    public static readonly ColorSet NoneOnDarkGray =
        new(ConsoleColor.DarkGray);

    public static readonly ColorSet NoneOnBlue =
        new(ConsoleColor.Blue);

    public static readonly ColorSet NoneOnGreen =
        new(ConsoleColor.Green);

    public static readonly ColorSet NoneOnCyan =
        new(ConsoleColor.Cyan);

    public static readonly ColorSet NoneOnRed =
        new(ConsoleColor.Red);

    public static readonly ColorSet NoneOnMagenta =
        new(ConsoleColor.Magenta);

    public static readonly ColorSet NoneOnYellow =
        new(ConsoleColor.Yellow);

    public static readonly ColorSet NoneOnWhite =
        new(ConsoleColor.White);
}
#endif
