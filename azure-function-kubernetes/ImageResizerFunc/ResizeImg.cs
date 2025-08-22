using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace ImageResizerFunc;

public class ResizeImg
{
    private readonly ILogger<ResizeImg> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    public ResizeImg(BlobServiceClient blobServiceClient, ILogger<ResizeImg> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    [Function("ResizeImage")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        _logger.LogInformation($"Content-Type: {req.Headers.GetValues("Content-Type").FirstOrDefault()}");
        try {

            // Parse multipart form data manually for HttpRequestData
            var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault();
            if (string.IsNullOrEmpty(contentType) || !contentType.Contains("multipart/form-data"))
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Request must be multipart/form-data");
                return errorResponse;
            }

            var boundary = GetBoundary(contentType);
            if (string.IsNullOrEmpty(boundary))
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid multipart boundary");
                return errorResponse;
            }

            var reader = new MultipartReader(boundary, req.Body);
            var section = await reader.ReadNextSectionAsync();
            
            IFormFile file = null;
            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out var contentDisposition);
                
                if (hasContentDispositionHeader && 
                    (contentDisposition.Name.Value == "\"image\"" || contentDisposition.Name.Value == "image"))
                {
                    var fileName = contentDisposition.FileName.HasValue ? 
                        contentDisposition.FileName.Value?.Trim('"') : null;
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var memoryStream = new MemoryStream();
                        await section.Body.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        
                        file = new FormFile(memoryStream, 0, memoryStream.Length, "image", fileName)
                        {
                            Headers = new HeaderDictionary(),
                            ContentType = section.ContentType
                        };
                    }
                    break;
                }
                section = await reader.ReadNextSectionAsync();
            }

            // Null validation to prevent NullReferenceException
            if (file == null)
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("No image file uploaded.");
                return errorResponse;
            }

            // Sanitize file name to prevent path traversal attacks
            var sanitizedFileName = SanitizeFileName(file.FileName);
            if (string.IsNullOrEmpty(sanitizedFileName))
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid file name.");
                return errorResponse;
            }

            // TODO: Uncomment when storage is configured
            var container = _blobServiceClient.GetBlobContainerClient("resized");
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlobClient(sanitizedFileName);
            using var stream = file.OpenReadStream();
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
            };
            await blob.UploadAsync(stream, uploadOptions);

            _logger.LogInformation($"File received: {sanitizedFileName}, Size: {file.Length} bytes");

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            // HTML encode filename to prevent XSS attacks
            var encodedFileName = HtmlEncoder.Default.Encode(sanitizedFileName);
            await response.WriteStringAsync($"Uploaded {encodedFileName} to resized container");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the request.");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An error occurred while processing the request.");
            return errorResponse;
        }    
    }

    private static string GetBoundary(string contentType)
    {
        var boundary = contentType.Split(';')
            .FirstOrDefault(x => x.Trim().StartsWith("boundary="))?.
            Split('=')[1]?.Trim();
        return boundary?.Trim('"');
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        // Remove any path traversal characters
        var sanitized = Path.GetFileName(fileName);
        var invalidChars = Path.GetInvalidFileNameChars();

        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        // Remove any remaining dangerous characters
        sanitized = sanitized.Replace("..", "").Replace("\\", "");
        return sanitized;
    }
}