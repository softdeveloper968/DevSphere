using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Azure.Identity;
using Azure.Storage.Queues;

namespace MyDMVpro.Common;


public class StorageHelpers
{
    // add helpers to save binary files to blob storage
    public static async Task<string> SaveFileToBlobAsync(string connectionString, string endpointUrl, string containerName, string fileName, byte[] fileData)
    {
        string blobUrl = string.Empty;

        try
        {
            var blobClient = GetBlobClient(connectionString, endpointUrl, containerName, fileName);

            using (MemoryStream stream = new MemoryStream(fileData))
            {
                await blobClient.UploadAsync(stream, true);
            }

            blobUrl = blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            throw;
        }

        return blobUrl;
    }
    private static BlobClient GetBlobClient(string connectionString, string endpointUrl, string containerName, string fileName)
    {
        BlobServiceClient blobServiceClient;

        if (!string.IsNullOrWhiteSpace(endpointUrl))
        {
            blobServiceClient = new BlobServiceClient(new Uri(endpointUrl), new DefaultAzureCredential());
        }
        else
        {
            blobServiceClient = new BlobServiceClient(connectionString);
        }
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        BlobClient blobClient = containerClient.GetBlobClient(fileName);
        return blobClient;
    }
    // add a helper to save a string to blob storage
    public static async Task<string> SaveStringToBlobAsync(string connectionString, string endpointUrl, string containerName, string fileName, string fileData)
    {
        string blobUrl = string.Empty;

        try
        {
            var blobClient = GetBlobClient(connectionString, endpointUrl, containerName, fileName);

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(fileData)))
            {
                await blobClient.UploadAsync(stream, true);
            }

            blobUrl = blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            throw;
        }

        return blobUrl;
    }
    // add a helper to read a blob as a byte array
    public static async Task<byte[]> GetBlobDataAsync(string connectionString, string endpointUrl, string containerName, string fileName)
    {
        byte[] fileData = null;
        try
        {
            var blobClient = GetBlobClient(connectionString, endpointUrl, containerName, fileName);

            using (MemoryStream stream = new MemoryStream())
            {
                await blobClient.DownloadToAsync(stream);
                fileData = stream.ToArray();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            throw;
        }

        return fileData;
    }
    // add a helper to add a message to a queue
    public static async Task SendMessageToQueueAsync(string connectionString, string endpointUrl, string queueName, string message)
    {
        try
        {
            QueueClient queueClient;
            QueueClientOptions options = new()
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };
            if (!string.IsNullOrWhiteSpace(endpointUrl))
            {
                queueClient = new QueueClient(new Uri(endpointUrl), new DefaultAzureCredential(), options);
            }
            else
            {
                queueClient = new QueueClient(connectionString, queueName, options);
            }
            await queueClient.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            throw;
        }
    }
}


