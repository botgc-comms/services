namespace BOTGC.MemberPortal.Common
{
    public static class ImageHelpers
    {
        public static string EncodePath(string relativePath)
        {
            var parts = relativePath
                .Replace("\\", "/", StringComparison.Ordinal)
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            return string.Join("/", parts.Select(Uri.EscapeDataString));
        }

        public static string GuessContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();

            return ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                ".bmp" => "image/bmp",
                ".tif" => "image/tiff",
                ".tiff" => "image/tiff",
                ".avif" => "image/avif",
                _ => "application/octet-stream"
            };
        }

        public static string NormaliseAssetKey(string fileName)
        {
            var s = (fileName ?? string.Empty).Replace("\\", "/", StringComparison.Ordinal).TrimStart('/');
            while (s.Contains("//", StringComparison.Ordinal))
            {
                s = s.Replace("//", "/", StringComparison.Ordinal);
            }

            return s;
        }

    }
}
