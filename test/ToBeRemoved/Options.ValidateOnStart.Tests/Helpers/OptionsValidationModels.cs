// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Options.Validation.Test;

#pragma warning disable SA1402 // File may only contain a single type

[OptionsValidator]
public partial class ModelValidator : IValidateOptions<Model>
{
}

[OptionsValidator]
public partial class Model2Validator : IValidateOptions<Model2>
{
}

[OptionsValidator]
public partial class ComplexModelValidator : IValidateOptions<ComplexModel>
{
}

[OptionsValidator]
public partial class InceptionComplexModelValidator : IValidateOptions<InceptionComplexModel>
{
}

public class Model
{
    [Range(1, 3)]
    public int Val { get; set; }
}

public class Model2
{
    [Range(1, 3)]
    public int Val1 { get; set; }

    [MinLength(1)]
    [MaxLength(5)]
    public string? Val2 { get; set; }
}

public class ModelWithoutOptionsValidator
{
    [Range(5, 10)]
    public int Val { get; set; }
}

public class ComplexModel
{
    [ValidateObjectMembers]
    public Model? ComplexVal { get; set; }

    [ValidateObjectMembers]
    public Model? ComplexValWithSameType { get; set; }

    [ValidateObjectMembers]
    public ModelWithoutOptionsValidator? ValWithoutOptionsValidator { get; set; }

#pragma warning disable R9G113
    public Model? ValWithoutRecursiveValidation { get; set; }
#pragma warning restore R9G113
}

public class InceptionComplexModel
{
    [Required]
    [ValidateObjectMembers]
    public ComplexModel? DeeplyComplexVal { get; set; }
}
