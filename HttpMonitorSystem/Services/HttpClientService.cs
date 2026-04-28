using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpMonitorSystem.Services
{
    public class HttpClientService
    {
        private readonly HttpClient _httpClient = new();

        public async Task<string> SendRequestAsync(string url, string method, string? jsonBody = null)
        {
            try
            {
                var request = new HttpRequestMessage(new HttpMethod(method), url);

                if (method.ToUpper() == "POST" && !string.IsNullOrEmpty(jsonBody))
                {
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                return $"Status: {(int)response.StatusCode} {response.StatusCode}\nBody:\n{responseBody}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}