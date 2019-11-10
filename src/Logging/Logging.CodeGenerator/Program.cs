using System;
using System.IO;

namespace Microsoft.Extensions.Logging.CodeGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var messagesPath = Path.Combine(Environment.CurrentDirectory, args[0]);

            foreach (var generated in MessageStructGenerator.Generate())
            {
                File.WriteAllText(Path.Combine(messagesPath, generated.fileName), generated.fileContent);
            }
        }
    }
}
