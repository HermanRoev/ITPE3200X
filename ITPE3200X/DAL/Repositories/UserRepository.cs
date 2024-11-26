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

        public async Task<bool> AddFollowerAsync(string followerUserId, string followedUserId)
        {
            try
            {
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

        public async Task<bool> IsFollowingAsync(string followerUserId, string followedUserId)
        {
            return await _context.Followers
                .AnyAsync(f => f.FollowerUserId == followerUserId && f.FollowedUserId == followedUserId);
        }
        
        public async Task<bool> UpdateProfileAsync(ApplicationUser user, string bio, IFormFile imageFile)
        {
            // Implementation for updating the profile
            user.Bio = bio;
            if (imageFile != null)
            {
                // Logic to handle the image file
                user.ProfilePictureUrl = "/path/to/new/picture.jpg"; // Example path
            }

            // Save changes to the database
            // Assuming _context is your database context
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}