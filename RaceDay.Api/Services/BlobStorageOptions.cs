namespace RaceDay.Api.Services
{
    /// <summary>Bound from the "AzureBlobStorage" section of configuration.</summary>
    public class BlobStorageOptions
    {
        public const string SectionName = "AzureBlobStorage";

        /// <summary>Azure Storage connection string. Use "UseDevelopmentStorage=true" for the Azurite emulator.</summary>
        public string ConnectionString { get; set; } = string.Empty;

        public string ContainerName { get; set; } = "raceday-images";

        /// <summary>
        /// Optional. When the API reaches storage on a different address than the browser does
        /// (for example "azurite" inside Docker vs "localhost" on the host), set this to the
        /// browser-reachable base address, e.g. http://localhost:10000/devstoreaccount1.
        /// Leave empty when using real Azure Storage.
        /// </summary>
        public string? PublicBaseUrl { get; set; }
    }
}
