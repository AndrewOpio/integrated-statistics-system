using System;

namespace VMSService.Core.Static
{
    public static class FileUrlHelper
    {
        public static string? ToWebPath(string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
                return storedPath;

            var normalized = storedPath.Replace('\\', '/');

            var uploadsIndex = normalized.IndexOf("/uploads/", StringComparison.OrdinalIgnoreCase);
            if (uploadsIndex >= 0)
            {
                return normalized.Substring(uploadsIndex);
            }

            if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return "/" + normalized;
            }

            return normalized;
        }
    }
}
