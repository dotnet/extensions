// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Gen.Metrics.Model;

/// <summary>
/// Path are the strings that go before the property name for child objects. When creating a strong type object for dimensions, the downstream properties must be written down.
/// </summary>
/// <example>
/// As an example:
/// <code language="csharp">
/// public class Dims
/// {
///     public string Dim1 { get; set; }
///     public ChildDims childDims { get; set; }
/// }
///
/// public class ChildDims
/// {
///     public string Dim2 { get; set; }
/// }
///
/// StrongTypeDimensionConfigs dim1 = new StrongTypeDimensionConfigs
/// {
///     Path = "",
///     DimensionName = "Dim1"
/// };
///
/// StrongTypeDimensionConfigs dim2 = new StrongTypeDimensionConfigs
/// {
///     Path = "childDims"
///     DimensionName = "Dim2"
/// }
/// </code>
/// </example>
internal sealed class StrongTypeConfig
{
    public string Path = string.Empty;
    public string Name = string.Empty;
    public string TagName = string.Empty;
    public StrongTypeMetricObjectType StrongTypeMetricObjectType { get; set; }
}
