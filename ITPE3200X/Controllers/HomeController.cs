using System.Diagnostics;
using ITPE3200X.DAL.Repositories;
using Microsoft.AspNetCore.Mvc;
using ITPE3200X.Models;
using Microsoft.AspNetCore.Identity;
using ITPE3200X.ViewModels;

namespace ITPE3200X.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IPostRepository _postRepository;
    private readonly IUserRepository _userRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public HomeController(
        ILogger<HomeController> logger,
        IPostRepository postRepository,
        IUserRepository userRepository,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment webHostEnvironment
    )
    {
        _logger = logger;
        _postRepository = postRepository;
        _userRepository = userRepository;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
    }

    // Main HomePage Action using HomeViewModel
    public async Task<IActionResult> HomePage()
    {
        var posts = await _postRepository.GetAllPostsAsync();
        var currentUserId = _userManager.GetUserId(User);

        // Create a list of PostViewModels based on the posts
        var postViewModels = posts.Select(p => new PostViewModel
        {
            PostId = p.PostId,
            Content = p.Content,
            Images = p.Images.ToList(),
            UserName = p.User.UserName,
            ProfilePicture = p.User.ProfilePictureUrl,
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
            IsSavedByCurrentUser = p.SavedPosts.Any(sp => sp.UserId == currentUserId),
            IsOwnedByCurrentUser = p.UserId == currentUserId,
            HomeFeed = true,
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count,
            Comments = p.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentViewModel
                {
                    IsCreatedByCurrentUser = c.UserId == currentUserId,
                    CommentId = c.CommentId,
                    UserName = c.User.UserName,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    TimeSincePosted = CalculateTimeSincePosted(c.CreatedAt)
                })
                .ToList()
        }).ToList();

        // Create the HomeViewModel with the list of posts
        var homeViewModel = new HomeViewModel
        {
            User = await _userManager.GetUserAsync(User), // Retrieve the current user if needed
            Posts = postViewModels, // Assign the list of PostViewModels
        };

        return View(homeViewModel); // Return the HomePage view with HomeViewModel
    }

    private string CalculateTimeSincePosted(DateTime createdAt)
    {
        var timeSpan = DateTime.UtcNow - createdAt;

        if (timeSpan.TotalMinutes < 60)
        {
            return $"{(int)timeSpan.TotalMinutes} m ago";
        }
        else if (timeSpan.TotalHours < 24)
        {
            return $"{(int)timeSpan.TotalHours} h ago";
        }
        else
        {
            return $"{(int)timeSpan.TotalDays} d ago";
        }
    }

    // Updated ToggleLike Method using HomePage functionality
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(string postId, bool isLike)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        // Retrieve the post
        var post = await _postRepository.GetPostByIdAsync(postId);
        if (post == null)
        {
            return NotFound();
        }

        if (isLike)
        {
            // Add like
            if (!post.Likes.Any(l => l.UserId == userId))
            {
                await _postRepository.AddLikeAsync(postId, userId);
            }
        }
        else
        {
            // Remove like
            await _postRepository.RemoveLikeAsync(postId, userId);
        }

        return RedirectToAction(nameof(HomePage)); // Refresh HomePage after the like action
    }

    // Updated ToggleSave Method using HomePage functionality
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSave(string postId, bool isSave)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        // Retrieve the post
        var post = await _postRepository.GetPostByIdAsync(postId);
        if (post == null)
        {
            return NotFound();
        }

        if (isSave)
        {
            // Add save
            if (!post.SavedPosts.Any(sp => sp.UserId == userId))
            {
                await _postRepository.AddSavedPost(postId, userId);
            }
        }
        else
        {
            // Remove save
            await _postRepository.RemoveSavedPost(postId, userId);
        }

        return RedirectToAction(nameof(HomePage)); // Refresh HomePage after the save action
    }

    // Updated AddComment Method using HomePage functionality
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(string postId, string content)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        // Validate content
        if (string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError("Content", "Comment cannot be empty.");
            return RedirectToAction(nameof(HomePage)); // Refresh page if validation fails
        }

        // Add the comment
        var comment = new Comment(postId, userId, content);
        await _postRepository.AddCommentAsync(comment);

        return RedirectToAction(nameof(HomePage)); // Refresh HomePage after adding the comment
    }

    // Updated DeleteComment Method using HomePage functionality
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(string postId, string commentId)
    {
        var userId = _userManager.GetUserId(User);

        await _postRepository.DeleteCommentAsync(commentId, userId);

        return RedirectToAction(nameof(HomePage)); // Refresh HomePage after deleting the comment
    }

    // Updated DeletePost Method using HomePage functionality
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(string postId)
    {
        var userId = _userManager.GetUserId(User);

        // Retrieve the post with images
        var post = await _postRepository.GetPostByIdAsync(postId);

        if (post == null || post.UserId != userId)
        {
            return NotFound();
        }

        // Delete image files from the file system
        foreach (var image in post.Images)
        {
            DeleteImageFile(image.ImageUrl);
        }

        // Delete the post (and associated images) from the database
        await _postRepository.DeletePostAsync(postId, userId);

        return RedirectToAction(nameof(HomePage)); // Refresh HomePage after deleting the post
    }

    private void DeleteImageFile(string imageUrl)
    {
        try
        {
            // Convert the image URL to a file path
            var wwwRootPath = _webHostEnvironment.WebRootPath;
            var filePath = Path.Combine(wwwRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, $"Error deleting image file: {imageUrl}");
        }
    }

    // Method for Saved Posts (remains unchanged)
    public IActionResult SavedPosts()
    {
        var userId = _userManager.GetUserId(User);

        var savedPosts = _userRepository.GetSavedPostsByUserIdAsync(userId).Result;

        var postViewModels = savedPosts.Select(p => new PostViewModel
        {
            PostId = p.PostId,
            Content = p.Content,
            Images = p.Images.ToList(),
            UserName = p.User.UserName,
            ProfilePicture = p.User.ProfilePictureUrl,
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == userId),
            IsSavedByCurrentUser = p.SavedPosts.Any(sp => sp.UserId == userId),
            IsOwnedByCurrentUser = p.UserId == userId,
            HomeFeed = false,
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count,
            Comments = p.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentViewModel
                {
                    IsCreatedByCurrentUser = c.UserId == userId,
                    CommentId = c.CommentId,
                    UserName = c.User.UserName,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    TimeSincePosted = CalculateTimeSincePosted(c.CreatedAt)
                })
                .ToList()
        }).ToList();

        return View(postViewModels);
    }
}