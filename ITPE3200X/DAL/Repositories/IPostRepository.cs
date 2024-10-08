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
        Task DeletePostAsync(string postId);

        // Comment methods
        Task AddCommentAsync(Comment comment);
        Task DeleteCommentAsync(string commentId);

        // Like methods
        Task AddLikeAsync(string postId, string userId);
        Task RemoveLikeAsync(string postId, string userId);
        Task<bool> IsPostLikedByUserAsync(string postId, string userId);
    }
}