using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Queues; // Namespace for Queue storage types
using Azure.Storage.Files.Shares; // Namespace for File Share storage types
using Microsoft.Extensions.Logging;

namespace AzureQueueDemo
{
    class Program
    {
        private const string storageConnectionString = "YourStorageAccountConnectionString";
        private const string queueName = "your-queue-name";
        private const string fileShareName = "your-file-share-name";
        private const string logFileName = "queue-logs.txt";

        static async Task Main(string[] args)
        {
            // Set up a logger
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            var logger = loggerFactory.CreateLogger<Program>();

            // Create a QueueClient object to interact with the queue
            QueueClient queueClient = new QueueClient(storageConnectionString, queueName);

            // Ensure the queue exists, if not, create it
            await queueClient.CreateIfNotExistsAsync();

            if (queueClient.Exists())
            {
                string message = "Hello, this is a test message!";

                // Send the message to the queue
                await queueClient.SendMessageAsync(message);

                logger.LogInformation($"Message '{message}' has been sent to the queue.");

                // Log to Azure File Share
                await LogToFileShare(logger, $"Successfully sent message: '{message}'", storageConnectionString, fileShareName);
            }
            else
            {
                logger.LogError($"Queue '{queueName}' does not exist.");
            }
        }

        private static async Task LogToFileShare(ILogger logger, string logMessage, string connectionString, string shareName)
        {
            try
            {
                ShareClient shareClient = new ShareClient(connectionString, shareName);

                // Ensure the file share exists
                await shareClient.CreateIfNotExistsAsync();

                // Create a directory if needed
                ShareDirectoryClient directoryClient = shareClient.GetRootDirectoryClient();

                // Create a file client
                ShareFileClient fileClient = directoryClient.GetFileClient(logFileName);

                // Check if the file exists
                if (!await fileClient.ExistsAsync())
                {
                    // Create the file with initial content
                    using MemoryStream stream = new MemoryStream();
                    StreamWriter writer = new StreamWriter(stream);
                    writer.WriteLine(logMessage);
                    writer.Flush();
                    stream.Position = 0;

                    await fileClient.CreateAsync(stream.Length);
                    await fileClient.UploadAsync(stream);
                }
                else
                {
                    // Append the log message to the existing file
                    ShareFileProperties properties = await fileClient.GetPropertiesAsync();
                    long newSize = properties.ContentLength + logMessage.Length + Environment.NewLine.Length;

                    using MemoryStream stream = new MemoryStream();
                    StreamWriter writer = new StreamWriter(stream);
                    writer.WriteLine(logMessage);
                    writer.Flush();
                    stream.Position = 0;

                    await fileClient.SetHttpHeadersAsync(newSize);
                    await fileClient.UploadRangeAsync(new HttpRange(properties.ContentLength, stream.Length), stream);
                }

                logger.LogInformation("Successfully logged to Azure File Share.");
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred while logging to Azure File Share: {ex.Message}");
            }
        }
    }
}
