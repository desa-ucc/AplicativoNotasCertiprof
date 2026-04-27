using Backend.Models;
using System.Threading.Tasks;

namespace Backend.Repositories
{
    public interface IUploadHistoryRepository
    {
        Task<UploadHistory?> GetByIdAsync(int id);
        Task AddAsync(UploadHistory uploadHistory);
        Task SaveChangesAsync();
    }
}
