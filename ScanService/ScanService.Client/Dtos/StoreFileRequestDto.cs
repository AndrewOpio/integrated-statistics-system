namespace ScanService.Client.Dtos
{
    /// <summary>
    /// Where the requesting microservice wants a cleared file to end up. The scan service only
    /// honours destinations that sit under one of its configured allowed roots.
    /// </summary>
    public class StoreFileRequestDto
    {
        /// <summary>
        /// Absolute directory chosen by the requesting microservice, e.g.
        /// <c>C:\CASFiles\uploads\passportphotos</c>. Created if it does not exist.
        /// </summary>
        public string DestinationDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Optional explicit file name. When omitted the scan service generates
        /// <c>{guid}_{originalName}</c>, matching what the microservices did before.
        /// </summary>
        public string? DestinationFileName { get; set; }

        /// <summary>Allow replacing an existing file with the same name. Defaults to false.</summary>
        public bool Overwrite { get; set; }
    }
}
