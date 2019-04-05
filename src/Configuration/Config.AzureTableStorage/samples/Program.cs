using System;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TestWithoutCancellationToken();
            TestWithCancellationToken();
        }

        private static void TestWithoutCancellationToken()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("settings.json");

            var config = builder.Build();
            
            builder.AddAzureTableStorage(config["StorageConnectionString"], "TestAzureTableStorageConfiguration", "TestAzureTableStorageConfigurationKey");

            try
            {
                config = builder.Build();
                Console.WriteLine($"Value for Config1 is {config["Config1"]}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception with message {ex.Message} thrown.");
            }
        }

        private static void TestWithCancellationToken()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("settings.json");

            var config = builder.Build();
            var token = new CancellationToken(true);    // Force cancel to see throw scenario
            
            builder.AddAzureTableStorage(config["StorageConnectionString"], token, "TestAzureTableStorageConfiguration", "TestAzureTableStorageConfigurationKey");

            try
            {
                config = builder.Build();

                Console.WriteLine($"Value for Config1 is {config["Config1"]}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception with message {ex.Message} thrown.");
            }
        }
    }
}
