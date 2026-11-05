using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Backend.Services
{
    public class PlannerService : IPlannerService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private string _accessToken;
        private DateTime _tokenExpiration;

        public PlannerService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiration)
            {
                return _accessToken;
            }

            var tenantId = _configuration["PlannerSettings:TenantId"] ?? Environment.GetEnvironmentVariable("PLANNER_TENANT_ID");
            var clientId = _configuration["PlannerSettings:ClientId"] ?? Environment.GetEnvironmentVariable("PLANNER_CLIENT_ID");
            var clientSecret = _configuration["PlannerSettings:ClientSecret"] ?? Environment.GetEnvironmentVariable("PLANNER_CLIENT_SECRET");

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                // Fallback for local testing if secrets are not set
                var generatedId = "PLANNER_TASK_MOCK_" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                return generatedId;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token");
            request.Content = new StringContent($"client_id={clientId}&scope=https%3A%2F%2Fgraph.microsoft.com%2F.default&client_secret={clientSecret}&grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            _accessToken = json.RootElement.GetProperty("access_token").GetString();
            int expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
            _tokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn - 60); // buffer 1 minute

            return _accessToken;
        }

        public async Task<string> CreateTaskAsync(string title, string bucketId = null, DateTime? dueDateTime = null)
        {
            var token = await GetAccessTokenAsync();

            if (token.StartsWith("PLANNER_TASK_MOCK_"))
            {
                // MOCK BEHAVIOR: Just return the fake token which actually acts as our mock ID when credentials aren't present.
                return token;
            }

            var planId = _configuration["PlannerSettings:PlanId"] ?? Environment.GetEnvironmentVariable("PLANNER_PLAN_ID");
            // If bucketId is not provided, use a default configured bucket.
            var defaultBucketId = bucketId ?? _configuration["PlannerSettings:DefaultBucketId"] ?? Environment.GetEnvironmentVariable("PLANNER_DEFAULT_BUCKET_ID");

            if (string.IsNullOrEmpty(planId) || string.IsNullOrEmpty(defaultBucketId))
            {
                throw new Exception("Planner PlanId or DefaultBucketId is not configured.");
            }

            var taskPayload = new
            {
                planId = planId,
                bucketId = defaultBucketId,
                title = title,
                dueDateTime = dueDateTime?.ToString("o") // ISO 8601 format
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/planner/tasks");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(JsonSerializer.Serialize(taskPayload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(responseContent);
            return json.RootElement.GetProperty("id").GetString();
        }
    }
}
