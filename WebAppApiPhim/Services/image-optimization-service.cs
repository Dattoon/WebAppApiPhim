using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace WebAppApiPhim.Services
{
    public interface IImageOptimizationService
    {
        Task<string> OptimizeAndCacheImageAsync(string imageUrl, ImageSize size);
        Task<byte[]> ResizeImageAsync(byte[] imageData, int width, int height, int quality = 80);
        Task<string> GenerateWebPVersionAsync(string imageUrl);
    }

    public class ImageOptimizationService : IImageOptimizationService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ImageOptimizationService> _logger;
        private readonly string _cacheDirectory;

        public ImageOptimizationService(
            HttpClient httpClient,
            IMemoryCache cache,
            ILogger<ImageOptimizationService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _cacheDirectory = configuration["ImageCache:Directory"] ?? "wwwroot/cache/images";

            Directory.CreateDirectory(_cacheDirectory);
        }

        public async Task<string> OptimizeAndCacheImageAsync(string imageUrl, ImageSize size)
        {
            if (string.IsNullOrEmpty(imageUrl)) return null;

            var cacheKey = $"img_{imageUrl.GetHashCode()}_{size}";

            if (_cache.TryGetValue(cacheKey, out string cachedPath))
            {
                return cachedPath;
            }

            try
            {
                var imageData = await DownloadImageAsync(imageUrl);
                if (imageData == null) return null;

                var (width, height) = GetDimensions(size);
                var optimizedData = await ResizeImageAsync(imageData, width, height);

                var fileName = $"{cacheKey}.jpg";
                var filePath = Path.Combine(_cacheDirectory, fileName);

                await File.WriteAllBytesAsync(filePath, optimizedData);

                var publicPath = $"/cache/images/{fileName}";
                _cache.Set(cacheKey, publicPath, TimeSpan.FromDays(7));

                return publicPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing image: {ImageUrl}", imageUrl);
                return imageUrl; // Return original URL as fallback
            }
        }

        public async Task<byte[]> ResizeImageAsync(byte[] imageData, int width, int height, int quality = 80)
        {
            using var image = SixLabors.ImageSharp.Image.Load(imageData);

            image.Mutate(x => x
                .Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center
                })
                .AutoOrient());

            using var output = new MemoryStream();
            var encoder = new JpegEncoder { Quality = quality };
            await image.SaveAsync(output, encoder);

            return output.ToArray();
        }

        public async Task<string> GenerateWebPVersionAsync(string imageUrl)
        {
            // Implementation for WebP conversion
            // This would convert images to WebP format for better compression
            throw new NotImplementedException();
        }

        private async Task<byte[]> DownloadImageAsync(string imageUrl)
        {
            try
            {
                using var response = await _httpClient.GetAsync(imageUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download image: {ImageUrl}", imageUrl);
            }
            return null;
        }

        private (int width, int height) GetDimensions(ImageSize size) => size switch
        {
            ImageSize.Thumbnail => (150, 225),
            ImageSize.Small => (300, 450),
            ImageSize.Medium => (500, 750),
            ImageSize.Large => (800, 1200),
            _ => (300, 450)
        };
    }

    public enum ImageSize
    {
        Thumbnail,
        Small,
        Medium,
        Large
    }
}
