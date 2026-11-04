using System.Threading.Tasks;

namespace Backend.Services
{
    public interface IPlannerService
    {
        Task<string> CreateTaskAsync(string title, string bucketId = null);
    }
}
