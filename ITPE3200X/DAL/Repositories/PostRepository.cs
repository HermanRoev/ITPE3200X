using ITPE3200X.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPE3200X.DAL.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly ApplicationDbContext _context;

        public PostRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Post methods
        public async Task<Post> GetPostByIdAsync(string postId)
        {
            var post = await _context.Posts
                .Include(p => p.Images)
                .Include(p => p.User)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.Likes)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PostId == postId);

            if (post == null)
            {
                throw new KeyNotFoundException($"Post with ID '{postId}' not found.");
            }

            return post;
        }

        public async Task<IEnumerable<Post>> GetAllPostsAsync()
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Images)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.Likes)
                .ThenInclude(l => l.User)
                .Include(p => p.SavedPosts)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task AddPostAsync(Post post)
        {
            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePostAsync(Post post, List<PostImage> imagesToDelete, List<PostImage> imagesToAdd)
        {
            if (imagesToDelete.Count > 0)
            {
                foreach (var image in imagesToDelete)
                {
                    _context.PostImages.Remove(image);
                }
            }
            if (imagesToAdd.Count > 0)
            {
                foreach (var image in imagesToAdd)
                {
                    await _context.PostImages.AddAsync(image);
                }
            }
            
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePostAsync(string postId, string userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post!.UserId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this post.");
            }
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
        }

        // Comment methods
        public async Task AddCommentAsync(Comment comment)
        {
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCommentAsync(string commentId, string userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            
            if (comment != null)
            {
                if (comment.UserId != userId)
                {
                    throw new UnauthorizedAccessException("You are not authorized to delete this comment.");
                }
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task EditCommentAsync(string commentId, string userId, string content)
        {
            var comment = await _context.Comments.FindAsync(commentId);

            if (comment != null)
            {
                if (comment.UserId != userId)
                {
                    throw new UnauthorizedAccessException("You are not authorized to edit this comment.");
                }

                comment.Content = content;
                await _context.SaveChangesAsync();
            }
        }

        // Like methods
        public async Task AddLikeAsync(string postId, string userId)
        {
            var like = new Like(postId, userId);
            await _context.Likes.AddAsync(like);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveLikeAsync(string postId, string userId)
        {
            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
            if (like != null)
            {
                _context.Likes.Remove(like);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddSavedPost(String postId, string userId)
        {
            var savedPost = new SavedPost(postId, userId);
            await _context.SavedPosts.AddAsync(savedPost);
            await _context.SaveChangesAsync();
        }
        
        public async Task RemoveSavedPost(String postId, string userId)
        {
            var savedPost = await _context.SavedPosts
                .FirstOrDefaultAsync(sp => sp.PostId == postId && sp.UserId == userId);
            if (savedPost != null)
            {
                _context.SavedPosts.Remove(savedPost);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Post>> GetPostsByUserAsync(string userId)
        {
            return await _context.Posts
                .Where(p => p.UserId == userId)
                .Include(p => p.User)
                .Include(p => p.Images)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.Likes)
                .ThenInclude(l => l.User)
                .Include(p => p.SavedPosts)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}