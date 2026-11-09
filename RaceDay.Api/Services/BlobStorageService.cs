using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services
{
    /// <summary>
    /// Stores images in an Azure Blob Storage container. The container is created on
    /// first use with public read access for blobs, so the browser can display the
    /// returned URLs directly (images only - no sensitive data is stored here).
    /// </summary>
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobStorageOptions _options;
        private BlobContainerClient? _container;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public BlobStorageService(IOptions<BlobStorageOptions> options)
        {
            _options = options.Value;
        }

        public async Task<string> UploadImageAsync(
            Stream content, string contentType, string folder, string extension, CancellationToken cancellationToken = default)
        {
            var container = await GetContainerAsync(cancellationToken);

            var blobName = $"{folder}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var blob = container.GetBlobClient(blobName);

            await blob.UploadAsync(
                content,
                new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            {
                return $"{_options.PublicBaseUrl.TrimEnd('/')}/{_options.ContainerName}/{blobName}";
            }

            return blob.Uri.ToString();
        }

        private async Task<BlobContainerClient> GetContainerAsync(CancellationToken cancellationToken)
        {
            if (_container != null) return _container;

            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new BlobStorageNotConfiguredException();
            }

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (_container == null)
                {
                    var service = new BlobServiceClient(_options.ConnectionString);
                    var container = service.GetBlobContainerClient(_options.ContainerName);
                    await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
                    _container = container;
                }
            }
            finally
            {
                _initLock.Release();
            }

            return _container;
        }
    }
}
