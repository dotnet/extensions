# About MediaTypeMap

This folder contains a polyfill for `System.Net.Mime.MediaTypeMap` which was added in .NET 10.
It provides methods for mapping between file extensions and media types (MIME types).

The implementation is a simplified version of the original that works on older frameworks
that don't have the `AlternateLookup` dictionary feature.

See: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Mail/src/System/Net/Mime/MediaTypeMap.cs
