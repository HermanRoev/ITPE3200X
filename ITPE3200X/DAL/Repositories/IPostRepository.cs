using ITPE3200X.Models;

namespace ITPE3200X.DAL.Repositories
{
    public interface IPostRepository
    {
        // Post methods
        Task<Post> GetPostByIdAsync(string postId);
        Task<IEnumerable<Post>> GetAllPostsAsync();
        Task AddPostAsync(Post post);
        Task UpdatePostAsync(Post post);
        Task DeletePostAsync(string postId, string userId);

        // Comment methods
        Task AddCommentAsync(Comment comment);
        Task DeleteCommentAsync(string commentId, string userId);

        // Like methods
        Task AddLikeAsync(string postId, string userId);
        Task RemoveLikeAsync(string postId, string userId);
        // Save methods
        Task AddSavedPost(String postId, string userId);
        Task RemoveSavedPost(String postId, string userId);
    }
}