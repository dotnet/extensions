# Memoization

Utility to provide simple and efficient function caching.

To use this in your project, add the following to your `.csproj` file:

```xml
<PropertyGroup>
  <InjectSharedMemoization>true</InjectSharedMemoization>
</PropertyGroup>
```

## Example

For example,

```csharp
async Task<int> Delay(int seconds)
{
    // Simulate a long-running/expensive function.
    await Task.Delay(TimeSpan.FromSeconds(seconds));
    return seconds;
}

Console.WriteLine($"t1 = {DateTimeOffset.UtcNow:o}");
await Delay(1);
await Delay(1);
Console.WriteLine($"t2 = {DateTimeOffset.UtcNow:o}");

var memoizedDelay = Memoize.Function<int, Task<int>>(Delay);

Console.WriteLine($"t3 = {DateTimeOffset.UtcNow:o}");
await memoizedDelay(1);
await memoizedDelay(1);
Console.WriteLine($"t4 = {DateTimeOffset.UtcNow:o}")
```

prints out

```text
t1 = 2021-10-22T21:57:12.3753431+00:00
t2 = 2021-10-22T21:57:14.3973958+00:00
t3 = 2021-10-22T21:57:14.4034649+00:00
t4 = 2021-10-22T21:57:15.4163199+00:00
```

## Notes

Memoize will use the equality of the types of the input parameters. This means that arbitrary objects
will use reference equality, unless those types define their own equality semantics. This implies that
callers should take care to use types with Memoize that would be safe to put in a Dictionary: if the
type is mutable, and its `Equals`/`GetHashCode` depends on those mutable parts, unexpected behavior may
result.
