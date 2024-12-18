using ITPE3200X.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPE3200X.DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        // Follower methods
        public async Task<bool> AddFollowerAsync(string followerUserId, string followedUserId)
        {
            try
            {
                // Check if the follower is already following the user
                var follower = new Follower(followerUserId, followedUserId);
                await _context.Followers.AddAsync(follower);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while adding a follower.");
                return false;
            }
        }
        
        // Follower methods
        public async Task<bool> RemoveFollowerAsync(string followerUserId, string followedUserId)
        {
            try
            {
                var follower = await _context.Followers.FirstOrDefaultAsync(f => f.FollowerUserId == followerUserId && f.FollowedUserId == followedUserId);
                
                if (follower == null)
                {
                    return false;
                }
                
                _context.Followers.Remove(follower);
                await _context.SaveChangesAsync();
                    
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while removing a follower.");
                return false;
            }
        }
        
        // Follower methods
        public async Task<bool> IsFollowingAsync(string followerUserId, string followedUserId)
        {
            return await _context.Followers
                .AnyAsync(f => f.FollowerUserId == followerUserId && f.FollowedUserId == followedUserId);
        }
    }
}