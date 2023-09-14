// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Defines a method to invoke to generate logging tags for a referenced object.
/// </summary>
/// <seealso cref="T:Microsoft.Extensions.Logging.LoggerMessageAttribute" />
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public sealed class TagProviderAttribute : Attribute
{
    /// <summary>
    /// Gets the <see cref="T:System.Type" /> containing the method that provides tags to be logged.
    /// </summary>
    public Type ProviderType { get; }

    /// <summary>
    /// Gets the name of the method that provides tags to be logged.
    /// </summary>
    public string ProviderMethod { get; }

    /// <summary>
    /// Gets or sets a value indicating whether to prefix the name of the parameter or property to the generated name of each tag being logged.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="false" />.
    /// </value>
    public bool OmitReferenceName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Logging.TagProviderAttribute" /> class with custom tags provider.
    /// </summary>
    /// <param name="providerType">A type containing a method that provides a custom set of tags to log.</param>
    /// <param name="providerMethod">The name of a method on the provider type which generates a custom set of tags to log.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// When <paramref name="providerMethod" /> or <paramref name="providerType" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// When <paramref name="providerMethod" /> is either an empty string or contains only whitespace.
    /// </exception>
    /// <remarks>
    /// You can create your own method that will generate the exact set of tags to log
    /// for a given input object.
    ///
    /// The method referenced by this constructor should be non-generic, <c>static</c>, <c>public</c> and it should have two parameters:
    /// <list type="number">
    ///   <item>
    ///     <description>First one of <see cref="T:Microsoft.Extensions.Logging.ITagCollector" /> type</description>
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
    /// [LoggerMessage(1, LogLevel.Warning, "Custom tags for {Param}.")]
    /// static partial void LogMethod(ILogger logger,
    ///     [TagProvider(typeof(CustomProvider), nameof(CustomProvider.GetTagsToLog))] ClassToLog o);
    ///
    /// public static class CustomProvider
    /// {
    ///     public static void GetTagsToLog(ITagCollector collector, ClassToLog? param)
    ///     {
    ///         collector.Add("Custom_tag_name", param?.MyProperty);
    ///         collector.Add(nameof(ClassToLog.AnotherProperty), param?.AnotherProperty);
    ///         // ...
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="T:Microsoft.Extensions.Logging.ITagCollector" />
    public TagProviderAttribute(Type providerType, string providerMethod);
}
