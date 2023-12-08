// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Defines a method to invoke to generate logging tags for a referenced object.
/// </summary>
/// <seealso cref="LoggerMessageAttribute"/>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class TagProviderAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TagProviderAttribute"/> class with custom tags provider.
    /// </summary>
    /// <param name="providerType">A type containing a method that provides a custom set of tags to log.</param>
    /// <param name="providerMethod">The name of a method on the provider type that generates a custom set of tags to log.</param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="providerMethod"/> or <paramref name="providerType"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <paramref name="providerMethod"/> is either an empty string or contains only whitespace.
    /// </exception>
    /// <remarks>
    /// You can create your own method that will generate the exact set of tags to log
    /// for a given input object.
    ///
    /// The method referenced by this constructor should be non-generic, <c>static</c>, and <c>public</c>, and it should have two parameters:
    /// <list type="bullet">
    ///   <item>
    ///     <description>First parameter of type <see cref="ITagCollector"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Second parameter of type <c>T?</c>, where <c>T</c> is the type of logging method parameter that you want to log.
    ///     </description>
    ///   </item>
    ///   </list>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
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
    /// <seealso cref="ITagCollector"/>
    public TagProviderAttribute(Type providerType, string providerMethod)
    {
        ProviderType = Throw.IfNull(providerType);
        ProviderMethod = Throw.IfNullOrWhitespace(providerMethod);
    }

    /// <summary>
    /// Gets the <see cref="Type"/> containing the method that provides tags to be logged.
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
    /// Defaults to <see langword="false"/>.
    /// </value>
    public bool OmitReferenceName { get; set; }
}
