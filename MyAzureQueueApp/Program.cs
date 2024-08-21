using System;
using System.Threading.Tasks;
using Azure.Storage.Queues; // Namespace for Queue storage types

namespace MyAzureQueApp
{
    class Program
    {
        // Connection string to your Azure Storage account
        private const string storageConnectionString = "YourStorageAccountConnectionString";
        
        // Name of your Queue
        private const string queueName = "your-queue-name";

        static async Task Main(string[] args)
        {
            // Create a QueueClient object to interact with the queue
            QueueClient queueClient = new QueueClient(storageConnectionString, queueName);

            // Ensure the queue exists, if not, create it
            await queueClient.CreateIfNotExistsAsync();

            if (queueClient.Exists())
            {
                string message = "Hello, this is a test message!";

                // Send the message to the queue
                await queueClient.SendMessageAsync(message);

                Console.WriteLine($"Message '{message}' has been sent to the queue.");
            }
            else
            {
                Console.WriteLine($"Queue '{queueName}' does not exist.");
            }
        }
    }
}
