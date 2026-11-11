# About FilePolyfills

This folder contains C# 14 extension member polyfills for `System.IO.File` methods
that are not available on older frameworks.

- `File.ReadAllBytesAsync` - Added in .NET Core 2.0, not available in .NET Framework 4.6.2 or .NET Standard 2.0

The polyfill uses C# 14 extension members so the call site can use `File.ReadAllBytesAsync` naturally
and it will use the real one on supported platforms and the polyfill elsewhere.
