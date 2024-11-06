using ITPE3200X.Models;

namespace ITPE3200X.DAL.Repositories
{
    public interface IPostRepository
    {
        // Post methods
        Task<Post> GetPostByIdAsync(string postId);
        Task<IEnumerable<Post>> GetAllPostsAsync();
        Task AddPostAsync(Post post);
        Task UpdatePostAsync(Post post, List<PostImage> imagesToDelete, List<PostImage> imagesToAdd);
        Task DeletePostAsync(string postId, string userId);

        // Comment methods
        Task AddCommentAsync(Comment comment);
        Task DeleteCommentAsync(string commentId, string userId);
        Task EditCommentAsync(string commentId, string userId, string content);

        // Like methods
        Task AddLikeAsync(string postId, string userId);
        Task RemoveLikeAsync(string postId, string userId);
        // Save methods
        Task AddSavedPost(String postId, string userId);
        Task RemoveSavedPost(String postId, string userId);
        Task<IEnumerable<Post>> GetPostsByUserAsync(string userId);
    }
}