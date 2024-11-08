using ITPE3200X.Models;

namespace ITPE3200X.DAL.Repositories
{
    public interface IPostRepository
    {
        // Post methods
        Task<Post?> GetPostByIdAsync(string postId);
        Task<IEnumerable<Post>?> GetSavedPostsByUserIdAsync(string userId);
        Task<IEnumerable<Post>?> GetAllPostsAsync();
        Task<IEnumerable<Post>> GetPostsByUserAsync(string userId);
        Task<bool> AddPostAsync(Post post);
        Task<bool> UpdatePostAsync(Post post, List<PostImage> imagesToDelete, List<PostImage> imagesToAdd);
        Task<bool> DeletePostAsync(string postId, string userId);

        // Comment methods
        Task<bool> AddCommentAsync(Comment comment);
        Task<bool> DeleteCommentAsync(string commentId, string userId);
        Task<bool> EditCommentAsync(string commentId, string userId, string content);

        // Like methods
        Task AddLikeAsync(string postId, string userId);
        Task RemoveLikeAsync(string postId, string userId);
        // Save methods
        Task AddSavedPost(String postId, string userId);
        Task RemoveSavedPost(String postId, string userId);
    }
}