using ITPE3200X.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPE3200X.DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        
        //Tina Special Lol prank haha tihi
       public async Task<ApplicationUser> GetUserdataById(String userId)
       {
           return await _context.Users
                       .Include(u => u.Followers)
                       .Include(u => u.Following)
                       .FirstOrDefaultAsync(u => u.Id == userId);
       }
        
       public async Task<bool> UpdateUserProfileAsync(string userId, string profilePictureUrl, string bio)
       {
           var user = await _context.Users.FindAsync(userId);
           if (user == null)
           {
               return false;
           }

           user.ProfilePictureUrl = profilePictureUrl;
           user.Bio = bio;

           _context.Users.Update(user);
           await _context.SaveChangesAsync();

           return true;
       }

        // Follower methods
        public async Task<IEnumerable<ApplicationUser>> GetFollowersAsync(string userId)
        {
            return await _context.Followers
                .Where(f => f.FollowedUserId == userId)
                .Select(f => f.FollowerUser)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<ApplicationUser>> GetFollowingAsync(string userId)
        {
            return await _context.Followers
                .Where(f => f.FollowerUserId == userId)
                .Select(f => f.FollowedUser)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddFollowerAsync(string followerUserId, string followedUserId)
        {
            var follower = new Follower(followerUserId, followedUserId);
            await _context.Followers.AddAsync(follower);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveFollowerAsync(string followerUserId, string followedUserId)
        {
            var follower = await _context.Followers
                .FirstOrDefaultAsync(f => f.FollowerUserId == followerUserId && f.FollowedUserId == followedUserId);
            if (follower != null)
            {
                _context.Followers.Remove(follower);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsFollowingAsync(string followerUserId, string followedUserId)
        {
            return await _context.Followers
                .AnyAsync(f => f.FollowerUserId == followerUserId && f.FollowedUserId == followedUserId);
        }

        // SavedPost methods
        public async Task<IEnumerable<Post>> GetSavedPostsByUserIdAsync(string userId)
        {
            var savedPosts = await _context.SavedPosts
                .Where(sp => sp.UserId == userId)
                .OrderByDescending(sp => sp.CreatedAt)
                .Include(sp => sp.Post)
                .ThenInclude(p => p.User)
                .Include(sp => sp.Post.Images)
                .Include(sp => sp.Post.Comments)
                .ThenInclude(c => c.User)
                .Include(sp => sp.Post.Likes)
                .ThenInclude(l => l.User)
                .AsNoTracking()
                .ToListAsync();

            var posts = savedPosts.Select(sp => sp.Post);

            return posts;
        }

        public async Task AddSavedPostAsync(string postId, string userId)
        {
            var savedPost = new SavedPost(postId, userId);
            await _context.SavedPosts.AddAsync(savedPost);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveSavedPostAsync(string postId, string userId)
        {
            var savedPost = await _context.SavedPosts
                .FirstOrDefaultAsync(sp => sp.PostId == postId && sp.UserId == userId);
            if (savedPost != null)
            {
                _context.SavedPosts.Remove(savedPost);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsPostSavedByUserAsync(string postId, string userId)
        {
            return await _context.SavedPosts
                .AnyAsync(sp => sp.PostId == postId && sp.UserId == userId);
        }
    }
}