using System;
using System.Threading.Tasks;

namespace Backend.Services
{
    public class PlannerService : IPlannerService
    {
        public Task<string> CreateTaskAsync(string title, string bucketId = null)
        {
            // Simulate calling Microsoft Graph API to create a task in Planner
            // and returning the generated Task ID.
            var generatedId = "PLANNER_TASK_" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            return Task.FromResult(generatedId);
        }
    }
}
