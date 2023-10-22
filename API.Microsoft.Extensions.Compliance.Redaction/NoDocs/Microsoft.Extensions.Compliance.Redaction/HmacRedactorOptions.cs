// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Compliance.Redaction;

[Experimental("EXTEXP0002", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class HmacRedactorOptions
{
    public int? KeyId { get; set; }
    [StringSyntax("Base64")]
    [Base64String]
    [Microsoft.Shared.Data.Validation.Length(44)]
    public string Key { get; set; }
    public HmacRedactorOptions();
}
