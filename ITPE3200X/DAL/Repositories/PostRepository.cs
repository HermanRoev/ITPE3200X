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
            return await _context.Posts
                .Include(p => p.Images)
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.Likes)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<IEnumerable<Post>> GetAllPostsAsync()
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Images)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task AddPostAsync(Post post)
        {
            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePostAsync(Post post)
        {
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePostAsync(string postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }

        // Comment methods
        public async Task AddCommentAsync(Comment comment)
        {
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCommentAsync(string commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
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

        public async Task<bool> IsPostLikedByUserAsync(string postId, string userId)
        {
            return await _context.Likes
                .AnyAsync(l => l.PostId == postId && l.UserId == userId);
        }
    }
}