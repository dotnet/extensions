// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace System.Cloud.DocumentDb;

public class TableOptions
{
    [Required]
    public string TableName { get; set; }
    [TimeSpan(1000)]
    public TimeSpan TimeToLive { get; set; }
    public string? PartitionIdPath { get; set; }
    public bool IsRegional { get; set; }
    public Throughput Throughput { get; set; }
    public bool IsLocatorRequired { get; set; }
    public TableOptions();
}
