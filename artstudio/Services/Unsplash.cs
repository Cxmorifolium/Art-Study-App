using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using artstudio.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IO;

namespace artstudio.Services
{
    public class Unsplash
    {
        private const string BaseUrl = "https://api.unsplash.com/";
        private readonly HttpClient _httpClient;
        private readonly string _accessKey;

        public Unsplash()
        {
            _accessKey = Environment.GetEnvironmentVariable("UNSPLASH_ACCESS_KEY")
                         ?? throw new Exception("Missing Unsplash API key. Set UNSPLASH_ACCESS_KEY as an environment variable.");

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Client-ID", _accessKey);
        }

        public async Task<List<UnsplashImage>> GetRandomImagesAsync(int count = 5)
        {
            var response = await _httpClient.GetAsync($"photos/random?count={count}");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Unsplash API error: {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();

            var images = JsonSerializer.Deserialize<List<UnsplashImage>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return images ?? new List<UnsplashImage>();
        }
    }
}
