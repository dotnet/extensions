using System;
using System.Reflection;
using System.Linq;

var asm = Assembly.LoadFrom(@"Q:\.tools\.nuget\packages\openai\2.2.0-beta.4\lib\net6.0\OpenAI.dll");
var realtimeTypes = asm.GetTypes()
    .Where(t => t.IsPublic && t.Namespace != null && t.Namespace.Contains("Realtime"))
    .OrderBy(t => t.FullName);
foreach (var t in realtimeTypes)
{
    Console.WriteLine(t.FullName);
}
