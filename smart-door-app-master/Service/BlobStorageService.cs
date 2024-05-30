using System.Text.Json;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Common.Models;

namespace Service;

public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }
    

    public async Task<string> GetImagesAsync()
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");
        var images = new List<ImageData>();
        var sasToken =
            @"sp=racwdli&st=2024-05-28T22:00:00Z&se=2024-05-30T22:00:00Z&sv=2022-11-02&sr=c&sig=0pdQqtN8IHamgFC7CnPvt3vBGV4JyL1vfyRCxt2PDNc%3D";

        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            var uri = $"{blobClient.Uri}?{sasToken}";
            images.Add(new ImageData { Url = uri.ToString(), FileName = blobItem.Name });
        }

        return JsonSerializer.Serialize(images);
    }

    public async Task<string> DeleteImageAsync(string fileName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");
        var blobClient = containerClient.GetBlobClient(fileName);

        if (await blobClient.ExistsAsync())
        {
            await blobClient.DeleteAsync();
            return "Image deleted";
        }

        return "Image not found";
    }
    
    public async Task<string> UploadImageAsync(string timestamp, byte[] imageData)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");
        var fileName = $"{timestamp}.jpg";
        var blobClient = containerClient.GetBlobClient(fileName);

        using (var stream = new MemoryStream(imageData))
        {
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "image/jpeg" });
        }

        return $"Image uploaded as {fileName}";
    }
}