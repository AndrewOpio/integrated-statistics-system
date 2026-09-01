namespace VMSService.Core.Static
{
    public static class FileStorageConstants
    {
        // Single source of truth for where uploaded files live on disk. Served at the "/uploads"
        // request path via a dedicated PhysicalFileProvider registration in Program.cs.
        public const string UploadsRoot = @"C:\CASFiles\uploads";
    }
}
