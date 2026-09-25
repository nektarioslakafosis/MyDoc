namespace MyDoc.Services
{
    public class AppointmentDocumentStorageService
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".jpg",
            ".jpeg",
            ".png"
        };

        private readonly IWebHostEnvironment _environment;

        public AppointmentDocumentStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return "Επιλέξτε αρχείο.";

            if (file.Length > MaxFileSizeBytes)
                return "Το αρχείο δεν μπορεί να ξεπερνά τα 5MB.";

            var extension = Path.GetExtension(file.FileName);

            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                return "Επιτρέπονται μόνο αρχεία PDF, JPG, JPEG ή PNG.";

            return null;
        }

        public async Task<SavedAppointmentDocumentFile> SaveAsync(IFormFile file)
        {
            var validationError = Validate(file);

            if (validationError != null)
                throw new InvalidOperationException(validationError);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var originalFileName = Path.GetFileName(file.FileName);

            if (string.IsNullOrWhiteSpace(originalFileName))
                originalFileName = "document" + extension;

            var filePath = Path.Combine(GetRootPath(), storedFileName);

            using var stream = new FileStream(filePath, FileMode.CreateNew);
            await file.CopyToAsync(stream);

            return new SavedAppointmentDocumentFile
            {
                OriginalFileName = originalFileName,
                StoredFileName = storedFileName,
                ContentType = GetContentTypeByExtension(extension),
                FileSize = file.Length
            };
        }

        public string GetFilePath(string storedFileName)
        {
            var safeStoredFileName = Path.GetFileName(storedFileName);
            return Path.Combine(GetRootPath(), safeStoredFileName);
        }

        private string GetRootPath()
        {
            var rootPath = Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "AppointmentDocuments");

            Directory.CreateDirectory(rootPath);

            return rootPath;
        }

        private static string GetContentTypeByExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }

    public class SavedAppointmentDocumentFile
    {
        public string OriginalFileName { get; set; } = string.Empty;

        public string StoredFileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }
    }
}