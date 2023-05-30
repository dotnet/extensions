// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Provides information to guide the production of a strongly-typed logging method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class LogMethodAttribute : Attribute
{
    /// <summary>
    /// Gets the logging event id for the logging method.
    /// </summary>
    /// <remarks>
    /// This is <c>0</c> if the logging method doesn't have a stable event id.
    /// </remarks>
    public int EventId { get; }

    /// <summary>
    /// Gets or sets the logging event name for the logging method.
    /// </summary>
    /// <remarks>
    /// This will equal the method name if not specified.
    /// </remarks>
    public string? EventName { get; set; }

    /// <summary>
    /// Gets the logging level for the logging method.
    /// </summary>
    public LogLevel? Level { get; }

    /// <summary>
    /// Gets the message text for the logging method.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the generated code should omit the logic to check whether a log level is enabled.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" /> if the log method's logging level is Error or Critical; otherwise the default value is <see langword="false" />.
    /// </value>
    /// <remarks>
    /// The generated code contains an optimization to avoid calling into the underlying <see cref="T:Microsoft.Extensions.Logging.ILogger" /> if the log method's log level
    /// is currently not enabled. If your application is already performing this check before calling the logging method, then you
    /// can remove the redundant check performed in the generated code by setting this option to <see langword="true" />.
    /// </remarks>
    public bool SkipEnabledCheck { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Logging.LogMethodAttribute" /> class
    /// which is used to guide the production of a strongly-typed logging method.
    /// </summary>
    /// <param name="eventId">The stable event id for this log message.</param>
    /// <param name="level">The logging level produced when invoking the strongly-typed logging method.</param>
    /// <param name="message">The message text output by the logging method. This string is a template that can contain any of the method's parameters.</param>
    /// <remarks>
    /// The method this attribute is applied to has some constraints
    ///   - Logging methods must be partial, and return void.
    ///   - Logging methods cannot be generic or accept any generic parameters.
    ///   - Logging method names must not start with an underscore.
    ///   - Parameter names of logging methods must not start with an underscore.
    ///   - If the logging method is static, one of its parameters must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />, or a type that implements the interface.
    ///   - If the logging method is an instance method, one of the fields of the containing type must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />.
    /// </remarks>
    /// <example>
    /// <code>
    /// static partial class Log
    /// {
    ///     [LogMethod(0, LogLevel.Critical, "Could not open socket for {hostName}")]
    ///     static partial void CouldNotOpenSocket(ILogger logger, string hostName);
    /// }
    /// </code>
    /// </example>
    public LogMethodAttribute(int eventId, LogLevel level, string message);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Logging.LogMethodAttribute" /> class
    /// which is used to guide the production of a strongly-typed logging method.
    /// </summary>
    /// <param name="eventId">The stable event id for this log message.</param>
    /// <param name="level">The logging level produced when invoking the strongly-typed logging method.</param>
    /// <remarks>
    /// The method this attribute is applied to has some constraints
    ///   - Logging methods must be partial, and return void.
    ///   - Logging methods cannot be generic or accept any generic parameters.
    ///   - Logging method names must not start with an underscore.
    ///   - Parameter names of logging methods must not start with an underscore.
    ///   - If the logging method is static, one of its parameters must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />, or a type that implements the interface.
    ///   - If the logging method is an instance method, one of the fields of the containing type must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />.
    /// </remarks>
    /// <example>
    /// <code>
    /// static partial class Log
    /// {
    ///     [LogMethod(0, LogLevel.Critical)]
    ///     static partial void CouldNotOpenSocket(ILogger logger, string hostName);
    /// }
    /// </code>
    /// </example>
    public LogMethodAttribute(int eventId, LogLevel level);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Logging.LogMethodAttribute" /> class
    /// which is used to guide the production of a strongly-typed logging method.
    /// </summary>
    /// <param name="level">The logging level produced when invoking the strongly-typed logging method.</param>
    /// <param name="message">The message text output by the logging method. This string is a template that can contain any of the method's parameters. Defaults to empty.</param>
    /// <remarks>
    /// The method this attribute is applied to has some constraints
    ///   - Logging methods must be partial, and return void.
    ///   - Logging methods cannot be generic or accept any generic parameters.
    ///   - Logging method names must not start with an underscore.
    ///   - Parameter names of logging methods must not start with an underscore.
    ///   - If the logging method is static, one of its parameters must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />, or a type that implements the interface.
    ///   - If the logging method is an instance method, one of the fields of the containing type must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />.
    ///
    /// This overload doesn't specify an event id, it is set to 0.
    /// </remarks>
    /// <example>
    /// <code>
    /// static partial class Log
    /// {
    ///     [LogMethod(LogLevel.Critical, "Could not open socket for {hostName}")]
    ///     static partial void CouldNotOpenSocket(ILogger logger, string hostName);
    /// }
    /// </code>
    /// </example>
    public LogMethodAttribute(LogLevel level, string message);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Logging.LogMethodAttribute" /> class
    /// which is used to guide the production of a strongly-typed logging method.
    /// </summary>
    /// <param name="level">The logging level produced when invoking the strongly-typed logging method.</param>
    /// <remarks>
    /// The method this attribute is applied to has some constraints
    ///   - Logging methods must be partial, and return void.
    ///   - Logging methods cannot be generic or accept any generic parameters.
    ///   - Logging method names must not start with an underscore.
    ///   - Parameter names of logging methods must not start with an underscore.
    ///   - If the logging method is static, one of its parameters must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />, or a type that implements the interface.
    ///   - If the logging method is an instance method, one of the fields of the containing type must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />.
    ///
    /// This overload doesn't specify an event id, it is set to 0.
    /// </remarks>
    /// <example>
    /// <code>
    /// static partial class Log
    /// {
    ///     [LogMethod(LogLevel.Critical)]
    ///     static partial void CouldNotOpenSocket(ILogger logger, string hostName);
    /// }
    /// </code>
    /// </example>
    public LogMethodAttribute(LogLevel level);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Logging.LogMethodAttribute" /> class
    /// which is used to guide the production of a strongly-typed logging method.
    /// </summary>
    /// <param name="message">The message text output by the logging method. This string is a template that can contain any of the method's parameters. Defaults to empty.</param>
    /// <remarks>
    /// The method this attribute is applied to has some constraints
    ///   - Logging methods must be partial, and return void.
    ///   - Logging methods cannot be generic or accept any generic parameters.
    ///   - Logging method names must not start with an underscore.
    ///   - Parameter names of logging methods must not start with an underscore.
    ///   - If the logging method is static, one of its parameters must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />, or a type that implements the interface.
    ///   - If the logging method is an instance method, one of the fields of the containing type must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />.
    ///
    /// This overload doesn't specify an event id, it is set to 0.
    /// </remarks>
    /// <example>
    /// <code>
    /// static partial class Log
    /// {
    ///     [LogMethod("Could not open socket for {hostName}")]
    ///     static partial void CouldNotOpenSocket(ILogger logger, LogLevel level, string hostName);
    /// }
    /// </code>
    /// </example>
    public LogMethodAttribute(string message);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Logging.LogMethodAttribute" /> class
    /// which is used to guide the production of a strongly-typed logging method.
    /// </summary>
    /// <param name="eventId">The stable event id for this log message.</param>
    /// <param name="message">The message text output by the logging method. This string is a template that can contain any of the method's parameters.</param>
    /// <remarks>
    /// This overload is not commonly used. In general, the overload that accepts a <see cref="T:Microsoft.Extensions.Logging.LogLevel" />
    /// value is preferred.
    ///
    /// The method this attribute is applied to has some constraints
    ///   - Logging methods must be partial, and return void.
    ///   - Logging methods cannot be generic or accept any generic parameters.
    ///   - Logging method names must not start with an underscore.
    ///   - Parameter names of logging methods must not start with an underscore.
    ///   - If the logging method is static, one of its parameters must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />, or a type that implements the interface.
    ///   - If the logging method is an instance method, one of the fields of the containing type must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />.
    /// </remarks>
    /// <example>
    /// <code>
    /// static partial class Log
    /// {
    ///     [LogMethod(0, "Could not open socket for {hostName}")]
    ///     static partial void CouldNotOpenSocket(ILogger logger, LogLevel level, string hostName);
    /// }
    /// </code>
    /// </example>
    public LogMethodAttribute(int eventId, string message);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Logging.LogMethodAttribute" /> class
    /// which is used to guide the production of a strongly-typed logging method.
    /// </summary>
    /// <param name="eventId">The stable event id for this log message.</param>
    /// <remarks>
    /// This overload is not commonly used. In general, the overload that accepts a <see cref="T:Microsoft.Extensions.Logging.LogLevel" />
    /// value is preferred.
    ///
    /// The method this attribute is applied to has some constraints
    ///   - Logging methods must be partial, and return void.
    ///   - Logging methods cannot be generic or accept any generic parameters.
    ///   - Logging method names must not start with an underscore.
    ///   - Parameter names of logging methods must not start with an underscore.
    ///   - If the logging method is static, one of its parameters must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />, or a type that implements the interface.
    ///   - If the logging method is an instance method, one of the fields of the containing type must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />.
    /// </remarks>
    /// <example>
    /// <code>
    /// static partial class Log
    /// {
    ///     [LogMethod(0)]
    ///     static partial void CouldNotOpenSocket(ILogger logger, LogLevel level, string hostName);
    /// }
    /// </code>
    /// </example>
    public LogMethodAttribute(int eventId);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Logging.LogMethodAttribute" /> class
    /// which is used to guide the production of a strongly-typed logging method.
    /// </summary>
    /// <remarks>
    /// This overload is not commonly used. In general, the overload that accepts a <see cref="T:Microsoft.Extensions.Logging.LogLevel" />
    /// value is preferred.
    ///
    /// The method this attribute is applied to has some constraints:
    ///   - Logging methods must be partial, and return void.
    ///   - Logging methods cannot be generic or accept any generic parameters.
    ///   - Logging method names must not start with an underscore.
    ///   - Parameter names of logging methods must not start with an underscore.
    ///   - If the logging method is static, one of its parameters must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />, or a type that implements the interface.
    ///   - If the logging method is an instance method, one of the fields of the containing type must be of type <see cref="T:Microsoft.Extensions.Logging.ILogger" />.
    ///
    /// This overload doesn't specify an event id, it is set to <c>0</c>, nor it specifies a message template - it is an empty string.
    /// </remarks>
    /// <example>
    /// <code>
    /// static partial class Log
    /// {
    ///     [LogMethod]
    ///     static partial void CouldNotOpenSocket(ILogger logger, LogLevel level, string hostName);
    /// }
    /// </code>
    /// </example>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public LogMethodAttribute();
}
