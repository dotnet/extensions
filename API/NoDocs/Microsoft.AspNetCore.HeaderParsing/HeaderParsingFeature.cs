// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Logging;
using Microsoft.Extensions.Telemetry.Metering;

namespace Microsoft.AspNetCore.HeaderParsing;

public sealed class HeaderParsingFeature
{
    public bool TryGetHeaderValue<T>(HeaderKey<T> header, [NotNullWhen(true)] out T? value) where T : notnull;
    public bool TryGetHeaderValue<T>(HeaderKey<T> header, [NotNullWhen(true)] out T? value, out ParsingResult result) where T : notnull;
}
