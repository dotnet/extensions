// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Marks a logging method parameter whose public properties need to be logged.
/// </summary>
/// <seealso cref="T:Microsoft.Extensions.Telemetry.Logging.LogMethodAttribute" />
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class LogPropertiesAttribute : Attribute
{
    /// <summary>
    /// Gets the <see cref="T:System.Type" /> containing the method that provides properties to be logged.
    /// </summary>
    public Type? ProviderType { get; }

    /// <summary>
    /// Gets the name of the method that provides properties to be logged.
    /// </summary>
    public string? ProviderMethod { get; }

    /// <summary>
    /// Gets or sets a value indicating whether <see langword="null" /> properties are logged.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="false" />.
    /// </value>
    public bool SkipNullProperties { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to prefix the name of the logging method parameter to the generated name of each property being logged.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="false" />.
    /// </value>
    public bool OmitParameterName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Logging.LogPropertiesAttribute" /> class.
    /// </summary>
    /// <remarks>
    /// Use this parameterless constructor if you want
    /// to get a source-generated set of properties to be logged.
    /// In case you need to provide your own set of properties or their custom names,
    /// use the <see cref="M:Microsoft.Extensions.Telemetry.Logging.LogPropertiesAttribute.#ctor(System.Type,System.String)" /> constructor overload instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// [LogMethod(1, LogLevel.Warning, "Logging complex object here.")]
    /// static partial void LogMethod(ILogger logger, [LogProperties] ClassToLog param);
    /// </code>
    /// </example>
    public LogPropertiesAttribute();

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Logging.LogPropertiesAttribute" /> class with custom properties provider.
    /// </summary>
    /// <param name="providerType">A type containing a method that provides a custom set of properties to log.</param>
    /// <param name="providerMethod">The name of a method on the provider type which generates a custom set of properties to log.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// When <paramref name="providerMethod" /> or <paramref name="providerType" /> are <see langword="null" />.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// When <paramref name="providerMethod" /> is either an empty string or contains only whitespace.
    /// </exception>
    /// <remarks>
    /// You can create your own method that will generate the exact set of properties to log
    /// for a given input object.
    ///
    /// Do NOT use this constructor overload if you want to have a default source-generated set of properties to log,
    /// Use the parameterless constructor in that case.
    ///
    /// The method referenced by this constructor should be non-generic, <c>static</c>, <c>public</c> and it should have two parameters:
    /// <list type="number">
    ///   <item>
    ///     <description>First one of <see cref="T:Microsoft.Extensions.Telemetry.Logging.ILogPropertyCollector" /> type</description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Second one of <c>T?</c> type, where <c>T</c> is a type of logging method parameter that you want to log.
    ///     </description>
    ///   </item>
    ///   </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// [LogMethod(1, LogLevel.Warning, "Custom properties for {Param}.")]
    /// static partial void LogMethod(ILogger logger,
    ///     [LogProperties(typeof(CustomProvider), nameof(CustomProvider.GetPropertiesToLog))] ClassToLog param);
    ///
    /// public static class CustomProvider
    /// {
    ///     public static void GetPropertiesToLog(ILogPropertyCollector props, ClassToLog? param)
    ///     {
    ///         props.Add("Custom_property_name", param?.MyProperty);
    ///         props.Add(nameof(ClassToLog.AnotherProperty), param?.AnotherProperty);
    ///         // ...
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="T:Microsoft.Extensions.Telemetry.Logging.ILogPropertyCollector" />
    public LogPropertiesAttribute(Type providerType, string providerMethod);
}
