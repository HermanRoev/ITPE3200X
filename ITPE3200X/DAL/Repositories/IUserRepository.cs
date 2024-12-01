using ITPE3200X.Models;

namespace ITPE3200X.DAL.Repositories
{
    public interface IUserRepository
    {
        // Follower methods
        Task<bool> AddFollowerAsync(string followerUserId, string followedUserId);
        Task<bool> RemoveFollowerAsync(string followerUserId, string followedUserId);
        Task<bool> IsFollowingAsync(string followerUserId, string followedUserId);
        Task<bool> UpdateProfileAsync(ApplicationUser user, string bio, IFormFile imageFile);
    }
}