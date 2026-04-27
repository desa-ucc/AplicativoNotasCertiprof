using Backend.Models;
using System.Threading.Tasks;

namespace Backend.Repositories
{
    public interface IUploadHistoryRepository
    {
        Task AddAsync(UploadHistory uploadHistory);
        Task SaveChangesAsync();
    }
}
