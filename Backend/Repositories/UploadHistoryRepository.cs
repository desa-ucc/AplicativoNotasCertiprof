using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Backend.Repositories
{
    public class UploadHistoryRepository : IUploadHistoryRepository
    {
        private readonly AppDbContext _dbContext;

        public UploadHistoryRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UploadHistory?> GetByIdAsync(int id)
        {
            return await _dbContext.UploadHistories
                .Include(h => h.Records)
                .FirstOrDefaultAsync(h => h.Id == id);
        }

        public async Task AddAsync(UploadHistory uploadHistory)
        {
            await _dbContext.UploadHistories.AddAsync(uploadHistory);
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
