# DisplayNameAttribute Support Analysis

## Problem Statement
> "We recently added DisplayNameAttribute support to AIFunctionFactory.Create, but that support only covered looking for DisplayName on methods. We also need to look for it on properties, using any DisplayName there to add a title to the relevant node in the schema. This should be possible using a transformer delegate as part of the schema creation options."

## Finding: Feature is Already Fully Implemented ✅

After thorough investigation, the requested feature is **already fully implemented** in the codebase.

## Implementation Details

### 1. DisplayNameAttribute on Methods
**Location**: `src/Libraries/Microsoft.Extensions.AI.Abstractions/Functions/AIFunctionFactory.cs` (line 732)
```csharp
Name = key.Name ?? key.Method.GetCustomAttribute<DisplayNameAttribute>(inherit: true)?.DisplayName ?? GetFunctionName(key.Method);
```

**Location**: `src/Libraries/Microsoft.Extensions.AI.Abstractions/Utilities/AIJsonUtilities.Schema.Create.cs` (line 83)
```csharp
title ??= method.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? method.Name;
```

### 2. DisplayNameAttribute on Properties
**Location**: `src/Libraries/Microsoft.Extensions.AI.Abstractions/Utilities/AIJsonUtilities.Schema.Create.cs` (lines 392-394)
```csharp
void ApplyDataAnnotations(ref JsonNode schema, AIJsonSchemaCreateContext ctx)
{
    if (ResolveAttribute<DisplayNameAttribute>() is { } displayNameAttribute)
    {
        ConvertSchemaToObject(ref schema)[TitlePropertyName] ??= displayNameAttribute.DisplayName;
    }
    // ... other data annotations (EmailAddress, Url, RegularExpression, etc.)
}
```

The `ApplyDataAnnotations` method is called for every schema node during schema generation (line 360), which includes properties. The `ResolveAttribute` helper method checks both parameter attributes and property attributes (via `ctx.GetCustomAttribute`).

### 3. User Customization via Transformer
**Location**: `src/Libraries/Microsoft.Extensions.AI.Abstractions/Utilities/AIJsonUtilities.Schema.Create.cs` (lines 362-366)
```csharp
// Finally, apply any user-defined transformations if specified.
if (inferenceOptions.TransformSchemaNode is { } transformer)
{
    schema = transformer(ctx, schema);
}
```

Users can customize the behavior by providing a `TransformSchemaNode` delegate in `AIJsonSchemaCreateOptions`, which is applied **after** `ApplyDataAnnotations`, allowing for override or extension of the default behavior.

## Example Usage

### Basic Usage
```csharp
using System.ComponentModel;
using Microsoft.Extensions.AI;

public class UserInfo
{
    [DisplayName("user_id")]
    [Description("The unique identifier")]
    public string UserId { get; set; }
    
    [DisplayName("user_age")]
    public int Age { get; set; }
    
    public string Name { get; set; } // No DisplayName
}

var func = AIFunctionFactory.Create((UserInfo input) => $"User: {input.Name}");
```

### Generated Schema
```json
{
  "type": "object",
  "properties": {
    "input": {
      "type": "object",
      "properties": {
        "userId": {
          "description": "The unique identifier",
          "type": ["string", "null"],
          "title": "user_id"
        },
        "age": {
          "type": "integer",
          "title": "user_age"
        },
        "name": {
          "type": ["string", "null"]
        }
      }
    }
  }
}
```

### Custom Transformer
```csharp
var options = new AIFunctionFactoryOptions
{
    JsonSchemaCreateOptions = new AIJsonSchemaCreateOptions
    {
        TransformSchemaNode = (ctx, schema) =>
        {
            // Custom logic to modify or override DisplayName behavior
            if (ctx.GetCustomAttribute<DisplayNameAttribute>() is { } attr)
            {
                // Custom handling
            }
            return schema;
        }
    }
};

var func = AIFunctionFactory.Create(method, options);
```

## Test Coverage

Added comprehensive test: `Metadata_DisplayNameAttributeOnProperties()` in `test/Libraries/Microsoft.Extensions.AI.Tests/Functions/AIFunctionFactoryTest.cs`

The test verifies:
- ✅ Properties with `DisplayNameAttribute` get a `title` field in the schema
- ✅ The title value matches the DisplayName value
- ✅ Properties without `DisplayNameAttribute` do not get a `title` field
- ✅ Works correctly with JSON serialization context

## Test Results
- All 70 AIFunctionFactory tests: **PASSED** ✅
- All 1062 AI.Abstractions tests: **PASSED** ✅

## Conclusion

The requested feature is fully implemented and working correctly:
1. DisplayNameAttribute on methods is supported
2. DisplayNameAttribute on properties is supported (automatically adds `title` to schema)
3. Users can customize via TransformSchemaNode callback
4. Comprehensive test coverage added to ensure continued functionality

No code changes were needed beyond adding the test to document and verify the feature.
