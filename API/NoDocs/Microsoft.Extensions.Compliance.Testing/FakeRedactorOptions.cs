// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Testing;

public class FakeRedactorOptions
{
    [Required]
    [StringSyntax("CompositeFormat")]
    public string RedactionFormat { get; set; }
    public FakeRedactorOptions();
}
