// Services/IFileUploadService.cs
using Microsoft.AspNetCore.Http;

namespace BookStoreMVC.Services
{
    public interface IFileUploadService
    {
        Task<FileUploadResult> UploadImageAsync(IFormFile file, string folder = "books");
        Task<MultipleFileUploadResult> UploadMultipleImagesAsync(IFormFileCollection files, string folder = "books/gallery");
        Task<bool> DeleteImageAsync(string imageUrl);
        Task<bool> DeleteMultipleImagesAsync(List<string> imageUrls);
        bool IsValidImageFile(IFormFile file);
        string GetImagePath(string fileName, string folder = "books");
        Task<FileUploadResult> ResizeAndUploadImageAsync(IFormFile file, string folder = "books", int maxWidth = 800, int maxHeight = 1200);
    }

    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string? ImageUrl { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long FileSize { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class MultipleFileUploadResult
    {
        public bool Success { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public List<string> FailedFiles { get; set; } = new();
        public List<string> ErrorMessages { get; set; } = new();
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly string[] _allowedContentTypes = {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
        };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<FileUploadResult> UploadImageAsync(IFormFile file, string folder = "books")
        {
            var result = new FileUploadResult();

            try
            {
                if (!IsValidImageFile(file))
                {
                    result.ErrorMessage = "Invalid image file. Please upload JPG, PNG, GIF, or WebP files only.";
                    return result;
                }

                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var uploadPath = Path.Combine(_environment.WebRootPath, "images", folder);
                Directory.CreateDirectory(uploadPath);
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                result.Success = true;
                result.ImageUrl = $"/images/{folder}/{fileName}";
                result.FileName = fileName;
                result.ContentType = file.ContentType;
                result.FileSize = file.Length;

                _logger.LogInformation("Image uploaded successfully: {FileName}", fileName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image: {FileName}", file.FileName);
                result.ErrorMessage = "An error occurred while uploading the image.";
                return result;
            }
        }

        public async Task<MultipleFileUploadResult> UploadMultipleImagesAsync(IFormFileCollection files, string folder = "books/gallery")
        {
            var result = new MultipleFileUploadResult();

            if (files == null || files.Count == 0)
            {
                result.ErrorMessages.Add("No files provided");
                return result;
            }

            foreach (var file in files)
            {
                try
                {
                    if (!IsValidImageFile(file))
                    {
                        result.FailedFiles.Add(file.FileName);
                        result.ErrorMessages.Add($"{file.FileName}: Invalid file format or size");
                        result.FailedCount++;
                        continue;
                    }

                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var uploadPath = Path.Combine(_environment.WebRootPath, "images", folder);
                    Directory.CreateDirectory(uploadPath);
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var imageUrl = $"/images/{folder}/{fileName}";
                    result.ImageUrls.Add(imageUrl);
                    result.SuccessCount++;

                    _logger.LogInformation("Gallery image uploaded: {FileName}", fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading gallery image: {FileName}", file.FileName);
                    result.FailedFiles.Add(file.FileName);
                    result.ErrorMessages.Add($"{file.FileName}: {ex.Message}");
                    result.FailedCount++;
                }
            }

            result.Success = result.SuccessCount > 0;
            return result;
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl) || imageUrl.StartsWith("http"))
                    return true;

                var relativePath = imageUrl.TrimStart('/');
                var filePath = Path.Combine(_environment.WebRootPath, relativePath);

                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    _logger.LogInformation("Image deleted: {FilePath}", filePath);
                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {ImageUrl}", imageUrl);
                return false;
            }
        }

        public async Task<bool> DeleteMultipleImagesAsync(List<string> imageUrls)
        {
            if (imageUrls == null || !imageUrls.Any())
                return true;

            var success = true;
            foreach (var imageUrl in imageUrls)
            {
                var deleted = await DeleteImageAsync(imageUrl);
                if (!deleted) success = false;
            }

            return success;
        }

        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > MaxFileSize)
                return false;

            if (!_allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return false;

            return true;
        }

        public string GetImagePath(string fileName, string folder = "books")
        {
            return $"/images/{folder}/{fileName}";
        }

        public async Task<FileUploadResult> ResizeAndUploadImageAsync(IFormFile file, string folder = "books", int maxWidth = 800, int maxHeight = 1200)
        {
            return await UploadImageAsync(file, folder);
        }
    }
}