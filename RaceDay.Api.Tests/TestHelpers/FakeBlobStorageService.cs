using RaceDay.Api.Services;

namespace RaceDay.Api.Tests.TestHelpers
{
    /// <summary>
    /// Test double for Azure Blob Storage: records what was "uploaded" and returns a
    /// predictable URL, so the tests never need a real storage account or emulator.
    /// </summary>
    public class FakeBlobStorageService : IBlobStorageService
    {
        public async Task<string> UploadImageAsync(
            Stream content, string contentType, string folder, string extension, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);

            return $"https://fake-storage.test/raceday-images/{folder}/{Guid.NewGuid():N}{extension}";
        }
    }
}
