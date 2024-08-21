using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

class Program
{
    private const string queueConnectionString = "<Your Azure Queue Storage connection string>";
    private const string blobConnectionString = "<Your Azure Blob Storage connection string>";
    private const string queueName = "<Your Queue Name>";
    private const string containerName = "<Your Blob Container Name>";
    private const string blobName = "<Your Blob File Name>";

    static async Task Main(string[] args)
    {
        // Initialize Queue Client
        QueueClient queueClient = new QueueClient(queueConnectionString, queueName);

        // Initialize Blob Client
        BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnectionString);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        // Ensure the blob container exists
        await containerClient.CreateIfNotExistsAsync();

        // Read messages from the queue
        QueueMessage[] retrievedMessages = await queueClient.ReceiveMessagesAsync(maxMessages: 10, visibilityTimeout: TimeSpan.FromSeconds(30));

        if (retrievedMessages.Length > 0)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (QueueMessage message in retrievedMessages)
            {
                // Process the message
                string messageContent = message.Body.ToString();
                stringBuilder.AppendLine(messageContent);

                // Delete the message from the queue after processing
                await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
            }

            // Upload the processed messages to the blob
            using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(stringBuilder.ToString())))
            {
                await blobClient.UploadAsync(memoryStream, overwrite: true);
            }

            Console.WriteLine("Messages written to blob successfully.");
        }
        else
        {
            Console.WriteLine("No messages found in the queue.");
        }
    }
}
