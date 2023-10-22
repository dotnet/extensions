// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// A redactor using HMACSHA256 to encode data being redacted.
/// </summary>
[Experimental("EXTEXP0002", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class HmacRedactorOptions
{
    /// <summary>
    /// Gets or sets the key ID.
    /// </summary>
    /// <value>
    /// Default set to <see langword="null" />.
    /// </value>
    /// <remarks>
    /// The key id is appended to each redacted value and is intended to identity the key that was used to hash the data.
    /// In general, every distinct key should have a unique id associated with it. When the hashed values have different key ids,
    /// it means the values are unrelated and can't be used for correlation.
    /// </remarks>
    public int? KeyId { get; set; }

    /// <summary>
    /// Gets or sets the hashing key.
    /// </summary>
    /// <remarks>
    /// The key is specified in base 64 format, and must be a minimum of 44 characters long.
    ///
    /// We recommend using a distinct key for each major deployment of a service (say for each region the service is in). Additionally,
    /// the key material should be kept secret, and rotated on a regular basis.
    /// </remarks>
    /// <value>
    /// Default set to <see cref="F:System.String.Empty" />.
    /// </value>
    [StringSyntax("Base64")]
    [Base64String]
    [Microsoft.Shared.Data.Validation.Length(44)]
    public string Key { get; set; }

    public HmacRedactorOptions();
}
