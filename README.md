# jsinterop

This repo is for `Microsoft.JSInterop`, a package that provides abstractions and features for interop between .NET and JavaScript code.

## Usage

The primary use case is for applications built with Mono WebAssembly or Blazor. It's not expected that developers will typically use these libraries separately from Mono WebAssembly, Blazor, or a similar technology.

## How to build and test

To build:

1. Ensure you have installed an up-to-date version of the [.NET Core SDK](https://www.microsoft.com/net/download). To verify, run `dotnet --version` and be sure that it returns `2.1.300` (i.e., .NET Core 2.1) or later.
2. Run `dotnet build`

To run tests:

1. Run `dotnet test test/Microsoft.JSInterop.Test`
