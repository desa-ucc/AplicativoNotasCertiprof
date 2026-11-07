using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Backend.Services
{
    public class PlannerService : IPlannerService
    {
        private readonly IConfiguration _configuration;
        private readonly GraphServiceClient _graphClient;
        private readonly bool _useMock;

        public PlannerService(IConfiguration configuration)
        {
            _configuration = configuration;

            var tenantId = _configuration["PlannerSettings:TenantId"] ?? Environment.GetEnvironmentVariable("GRAPH_TENANT_ID");
            var clientId = _configuration["PlannerSettings:ClientId"] ?? Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
            var clientSecret = _configuration["PlannerSettings:ClientSecret"] ?? Environment.GetEnvironmentVariable("GRAPH_CLIENT_SECRET");

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || tenantId == "your_tenant_id_here")
            {
                // Fallback for local testing if secrets are not set
                _useMock = true;
            }
            else
            {
                var options = new TokenCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                };

                var clientSecretCredential = new ClientSecretCredential(
                    tenantId, clientId, clientSecret, options);

                _graphClient = new GraphServiceClient(clientSecretCredential);
                _useMock = false;
            }
        }

        public async Task<string> CreateTaskAsync(string title, string bucketId = null, DateTime? dueDateTime = null)
        {
            if (_useMock)
            {
                return "PLANNER_TASK_MOCK_" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            }

            var planId = _configuration["PlannerSettings:PlanId"] ?? Environment.GetEnvironmentVariable("PLANNER_PLAN_ID");
            var defaultBucketId = bucketId ?? _configuration["PlannerSettings:DefaultBucketId"] ?? Environment.GetEnvironmentVariable("PLANNER_DEFAULT_BUCKET_ID");

            if (string.IsNullOrEmpty(planId) || string.IsNullOrEmpty(defaultBucketId))
            {
                throw new Exception("Planner PlanId or DefaultBucketId is not configured.");
            }

            var plannerTask = new PlannerTask
            {
                PlanId = planId,
                BucketId = defaultBucketId,
                Title = title,
            };

            if (dueDateTime.HasValue)
            {
                plannerTask.DueDateTime = new DateTimeOffset(dueDateTime.Value);
            }

            var result = await _graphClient.Planner.Tasks.PostAsync(plannerTask);
            return result?.Id;
        }
    }
}