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

        public string FileSizeFormatted
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                else if (FileSize < 1024 * 1024)
                    return $"{(FileSize / 1024.0):F2} KB";
                else
                    return $"{(FileSize / (1024.0 * 1024.0)):F2} MB";
            }
        }

        public string FileIcon
        {
            get
            {
                var extension = System.IO.Path.GetExtension(FileName).ToLower();
                return extension switch
                {
                    ".pdf" => "ti-file-type-pdf",
                    ".doc" or ".docx" => "ti-file-type-doc",
                    ".xls" or ".xlsx" => "ti-file-type-xls",
                    ".png" or ".jpg" or ".jpeg" => "ti-photo",
                    _ => "ti-file"
                };
            }
        }

        public string FileColor
        {
            get
            {
                var extension = System.IO.Path.GetExtension(FileName).ToLower();
                return extension switch
                {
                    ".pdf" => "text-danger",
                    ".doc" or ".docx" => "text-primary",
                    ".xls" or ".xlsx" => "text-success",
                    ".png" or ".jpg" or ".jpeg" => "text-info",
                    _ => "text-secondary"
                };
            }
        }

        // Add these new properties
        public bool IsPdf
        {
            get
            {
                return FileType.ToLower().Contains("pdf") ||
                       System.IO.Path.GetExtension(FileName).ToLower() == ".pdf";
            }
        }

        public bool IsImage
        {
            get
            {
                var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
                return FileType.ToLower().Contains("image") ||
                       imageExtensions.Contains(System.IO.Path.GetExtension(FileName).ToLower());
            }
        }

        public bool IsPreviewable => IsPdf || IsImage;
    }
}