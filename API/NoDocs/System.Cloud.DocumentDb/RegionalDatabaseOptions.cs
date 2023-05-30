// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

public class RegionalDatabaseOptions
{
    public string? DatabaseName { get; set; }
    [Required]
    public Uri? Endpoint { get; set; }
    public string? PrimaryKey { get; set; }
    public IList<string> FailoverRegions { get; set; }
    public RegionalDatabaseOptions();
}
