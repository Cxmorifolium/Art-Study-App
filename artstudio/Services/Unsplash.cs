using artstudio.Models;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace artstudio.Services
{
    public partial class Unsplash : IDisposable
    {
        private const string BaseUrl = "https://api.unsplash.com/";
        private readonly HttpClient _httpClient;
        private readonly string _accessKey;
        private readonly ILogger<Unsplash> _logger;
        private bool _disposed = false;

        // Cache JsonSerializerOptions to avoid recreating on every operation
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public Unsplash(ILogger<Unsplash> logger)
        {
            _logger = logger;
            _accessKey = Environment.GetEnvironmentVariable("UNSPLASH_ACCESS_KEY")
                         ?? throw new InvalidOperationException("Missing Unsplash API key. Set UNSPLASH_ACCESS_KEY as an environment variable.");

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30) // Add timeout
            };
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Client-ID", _accessKey);
        }

        public async Task<List<UnsplashImage>> GetRandomImagesAsync(int count = 5)
        {
            if (count <= 0 || count > 30) // Unsplash API limit
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be between 1 and 30");
            }

            try
            {
                _logger.LogDebug("Requesting {Count} random images from Unsplash API", count);

                using var response = await _httpClient.GetAsync($"photos/random?count={count}");

                // Handle specific HTTP status codes
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        _logger.LogDebug("Successfully received response from Unsplash API");
                        break;
                    case HttpStatusCode.Unauthorized:
                        _logger.LogError("Unauthorized access to Unsplash API - invalid API key");
                        throw new UnauthorizedAccessException("Invalid Unsplash API key or insufficient permissions");
                    case HttpStatusCode.Forbidden:
                        _logger.LogError("Forbidden access to Unsplash API - check permissions");
                        throw new InvalidOperationException("Access forbidden. Check API key permissions");
                    case HttpStatusCode.TooManyRequests:
                        _logger.LogWarning("Unsplash API rate limit exceeded");
                        throw new InvalidOperationException("Rate limit exceeded. Please try again later");
                    case HttpStatusCode.InternalServerError:
                    case HttpStatusCode.BadGateway:
                    case HttpStatusCode.ServiceUnavailable:
                    case HttpStatusCode.GatewayTimeout:
                        _logger.LogWarning("Unsplash service temporarily unavailable: {StatusCode}", response.StatusCode);
                        throw new HttpRequestException($"Unsplash service is temporarily unavailable (Status: {response.StatusCode})");
                    default:
                        _logger.LogError("Unsplash API request failed with status: {StatusCode}", response.StatusCode);
                        throw new HttpRequestException($"Unsplash API request failed with status: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogError("Empty response received from Unsplash API");
                    throw new InvalidOperationException("Empty response received from Unsplash API");
                }

                var images = DeserializeImages(content);
                _logger.LogDebug("Successfully parsed {ImageCount} images from Unsplash API response", images.Count);
                return images;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Request to Unsplash API timed out");
                throw new TimeoutException("Request to Unsplash API timed out", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while contacting Unsplash API");
                throw new HttpRequestException($"Network error while contacting Unsplash API: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse response from Unsplash API");
                throw new InvalidOperationException($"Failed to parse response from Unsplash API: {ex.Message}", ex);
            }
        }

        private List<UnsplashImage> DeserializeImages(string content)
        {
            try
            {
                var apiImages = JsonSerializer.Deserialize<List<UnsplashApiResponse>>(content, JsonOptions);

                if (apiImages == null || apiImages.Count == 0)
                {
                    _logger.LogDebug("No images found in API response");
                    return [];
                }

                var images = new List<UnsplashImage>();
                foreach (var apiImage in apiImages)
                {
                    try
                    {
                        var convertedImage = ConvertToUnsplashImage(apiImage);
                        if (IsValidImage(convertedImage))
                        {
                            images.Add(convertedImage);
                        }
                        else
                        {
                            _logger.LogDebug("Skipped invalid image: {ImageId}", apiImage?.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue processing other images
                        _logger.LogWarning(ex, "Failed to convert image {ImageId}", apiImage?.Id);
                    }
                }

                return images;
            }
            catch (JsonException)
            {
                throw; // Re-throw JSON exceptions to be handled by caller
            }
        }

        private static UnsplashImage ConvertToUnsplashImage(UnsplashApiResponse apiResponse)
        {
            ArgumentNullException.ThrowIfNull(apiResponse, nameof(apiResponse));

            return new UnsplashImage
            {
                Id = apiResponse.Id ?? throw new InvalidOperationException("Image ID is required"),
                Description = apiResponse.Description ?? apiResponse.Alt_Description ?? "No description available",
                urls = apiResponse.Urls != null ? new UnsplashImage.Urls
                {
                    Raw = apiResponse.Urls.Raw,
                    Full = apiResponse.Urls.Full,
                    Regular = apiResponse.Urls.Regular,
                    Small = apiResponse.Urls.Small,
                    Thumb = apiResponse.Urls.Thumb
                } : throw new InvalidOperationException("Image URLs are required"),
                user = apiResponse.User != null ? new UnsplashImage.User
                {
                    Name = apiResponse.User.Name ?? "Unknown",
                    PortfolioUrl = apiResponse.User.Portfolio_Url
                } : new UnsplashImage.User { Name = "Unknown" }
            };
        }

        private static bool IsValidImage(UnsplashImage image)
        {
            return !string.IsNullOrEmpty(image.Id) &&
                   image.urls != null &&
                   !string.IsNullOrEmpty(image.urls.Regular);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }

    // API Response models that match the exact JSON structure from Unsplash
    internal class UnsplashApiResponse
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public string? Alt_Description { get; set; }
        public ApiUrls? Urls { get; set; }
        public ApiUser? User { get; set; }
    }

    internal class ApiUrls
    {
        public string? Raw { get; set; }
        public string? Full { get; set; }
        public string? Regular { get; set; }
        public string? Small { get; set; }
        public string? Thumb { get; set; }
    }

    internal class ApiUser
    {
        public string? Name { get; set; }
        public string? Portfolio_Url { get; set; }
    }
}