// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Options to configure generated REST API clients.
/// </summary>
/// <remarks>
/// This options class is used to configure generated REST API clients.
/// </remarks>
/// <example>
/// <code>
/// services.AddMyDependencyClient(options =>
/// {
///     options.JsonSerializerOptions = new MyJsonSerializerOptions();
/// });
/// </code>
/// </example>
public class AutoClientOptions
{
    /// <summary>
    /// Gets or sets JSON payload serialization options.
    /// </summary>
    [Required]
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new();
}
