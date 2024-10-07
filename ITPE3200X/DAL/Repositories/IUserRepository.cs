using ITPE3200X.Models;

namespace ITPE3200X.DAL.Repositories
{
    public interface IUserRepository
    {
        // Follower methods
        Task<IEnumerable<ApplicationUser>> GetFollowersAsync(string userId);
        Task<IEnumerable<ApplicationUser>> GetFollowingAsync(string userId);
        Task AddFollowerAsync(string followerUserId, string followedUserId);
        Task RemoveFollowerAsync(string followerUserId, string followedUserId);
        Task<bool> IsFollowingAsync(string followerUserId, string followedUserId);

        // SavedPost methods
        Task<IEnumerable<Post>> GetSavedPostsByUserIdAsync(string userId);
        Task AddSavedPostAsync(string postId, string userId);
        Task RemoveSavedPostAsync(string postId, string userId);
        Task<bool> IsPostSavedByUserAsync(string postId, string userId);
    }
}