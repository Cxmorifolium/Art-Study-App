using artstudio.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace artstudio.Services
{
    public class Unsplash : IDisposable
    {
        private const string BaseUrl = "https://api.unsplash.com/";
        private readonly HttpClient _httpClient;
        private readonly string _accessKey;
        private bool _disposed = false;

        public Unsplash()
        {
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
                using var response = await _httpClient.GetAsync($"photos/random?count={count}");

                // Handle specific HTTP status codes
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        break;
                    case HttpStatusCode.Unauthorized:
                        throw new UnauthorizedAccessException("Invalid Unsplash API key or insufficient permissions");
                    case HttpStatusCode.Forbidden:
                        throw new InvalidOperationException("Access forbidden. Check API key permissions");
                    case HttpStatusCode.TooManyRequests:
                        throw new InvalidOperationException("Rate limit exceeded. Please try again later");
                    case HttpStatusCode.InternalServerError:
                    case HttpStatusCode.BadGateway:
                    case HttpStatusCode.ServiceUnavailable:
                    case HttpStatusCode.GatewayTimeout:
                        throw new HttpRequestException($"Unsplash service is temporarily unavailable (Status: {response.StatusCode})");
                    default:
                        throw new HttpRequestException($"Unsplash API request failed with status: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new InvalidOperationException("Empty response received from Unsplash API");
                }

                return DeserializeImages(content);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                throw new TimeoutException("Request to Unsplash API timed out", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException($"Network error while contacting Unsplash API: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse response from Unsplash API: {ex.Message}", ex);
            }
        }

        private List<UnsplashImage> DeserializeImages(string content)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };

                var apiImages = JsonSerializer.Deserialize<List<UnsplashApiResponse>>(content, options);

                if (apiImages == null || !apiImages.Any())
                {
                    return new List<UnsplashImage>();
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
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue processing other images
                        Console.WriteLine($"Warning: Failed to convert image {apiImage?.Id}: {ex.Message}");
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
            if (apiResponse == null)
            {
                throw new ArgumentNullException(nameof(apiResponse));
            }

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