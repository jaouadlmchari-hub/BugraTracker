using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTracker.Infrastructure.Persistence.Repositories
{
    public class RefreshTokenRepository : Repository<RefreshToken> , IRefreshTokenRepository
    {

        public RefreshTokenRepository(BugTrackerDbContext context) : base(context) { }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _dbSet
                         .FirstOrDefaultAsync(t => t.Token == token);
        }

        public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                         .Where(t => t.UserId == userId)
                         .ToListAsync();
        }

        public async Task RevokeAsync(Guid tokenId)
        {
            var token = await _dbSet.FindAsync(tokenId);

            if (token != null)
            {
                token.IsRevoked = true;
            }
        }
    }
}
