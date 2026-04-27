using Backend.Models;
using System.Threading.Tasks;

namespace Backend.Repositories
{
    public interface IUploadHistoryRepository
    {
        Task<UploadHistory?> GetByIdAsync(int id);
        Task<System.Collections.Generic.IEnumerable<UploadHistory>> GetAllAsync();
        Task<System.Collections.Generic.IEnumerable<UploadHistory>> GetByUsernameAsync(string username);
        Task AddAsync(UploadHistory uploadHistory);
        Task SaveChangesAsync();
    }
}
