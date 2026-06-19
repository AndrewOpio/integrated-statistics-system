namespace VMSService.Core.Models.Attachments
{
    public class AttachmentViewModel
    {
        public long Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedDate { get; set; }

        public string FileSizeFormatted => FileSize < 1024
            ? $"{FileSize} B"
            : FileSize < 1024 * 1024
                ? $"{(FileSize / 1024.0):F2} KB"
                : $"{(FileSize / (1024.0 * 1024.0)):F2} MB";

        public string FileIcon => Path.GetExtension(FileName).ToLower() switch
        {
            ".pdf"            => "ti-file-type-pdf",
            ".doc" or ".docx" => "ti-file-type-doc",
            ".xls" or ".xlsx" => "ti-file-type-xls",
            ".png" or ".jpg" or ".jpeg" => "ti-photo",
            _                 => "ti-file"
        };

        public string FileColor => Path.GetExtension(FileName).ToLower() switch
        {
            ".pdf"            => "text-danger",
            ".doc" or ".docx" => "text-primary",
            ".xls" or ".xlsx" => "text-success",
            ".png" or ".jpg" or ".jpeg" => "text-info",
            _                 => "text-secondary"
        };

        public bool IsPdf => FileType.ToLower().Contains("pdf") ||
                             Path.GetExtension(FileName).ToLower() == ".pdf";

        public bool IsImage => new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp" }
                             .Contains(Path.GetExtension(FileName).ToLower());

        public bool IsPreviewable => IsPdf || IsImage;
    }
}
