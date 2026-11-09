namespace RaceDay.Api.Services
{
    /// <summary>
    /// Abstraction over Azure Blob Storage. Controllers depend on this interface
    /// (not on the Azure SDK) so the storage can be replaced with a fake in unit tests.
    /// All image handling in RaceDay goes through the API - the MVC application never
    /// talks to Azure directly.
    /// </summary>
    public interface IBlobStorageService
    {
        /// <summary>
        /// Uploads an image and returns its public URL.
        /// </summary>
        /// <param name="content">The image bytes.</param>
        /// <param name="contentType">MIME type, e.g. image/png.</param>
        /// <param name="folder">Virtual folder inside the container, e.g. "banners" or "profiles".</param>
        /// <param name="extension">File extension including the dot, e.g. ".png".</param>
        Task<string> UploadImageAsync(Stream content, string contentType, string folder, string extension, CancellationToken cancellationToken = default);
    }

    /// <summary>Thrown when Azure Blob Storage has not been configured for this environment.</summary>
    public class BlobStorageNotConfiguredException : Exception
    {
        public BlobStorageNotConfiguredException()
            : base("Azure Blob Storage is not configured. Set AzureBlobStorage:ConnectionString.") { }
    }
}
