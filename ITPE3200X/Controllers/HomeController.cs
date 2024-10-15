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
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, IPostRepository postRepository, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _postRepository = postRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var posts = await _postRepository.GetAllPostsAsync();
        
        var currentUserId = _userManager.GetUserId(User);
        
        var postViewModels = posts.Select(p => new PostViewModel
        {
            PostId = p.PostId,
            Content = p.Content,
            Images = p.Images.ToList(),
            UserName = p.User.UserName,
            ProfilePicture = p.User.ProfilePictureUrl ?? "/images/default-profile.png",
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
            IsSavedByCurrentUser = p.SavedPosts.Any(sp => sp.UserId == currentUserId),
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count,
            Comments = p.Comments,
        }).ToList();
        
        return View(postViewModels);
    }

    public async Task<PostViewModel> GetPostViewModelById(String postId)
    {
        var post = await _postRepository.GetPostByIdAsync(postId);
        
        var currentUserId = _userManager.GetUserId(User);

        return new PostViewModel
        {
            PostId = post.PostId,
            Content = post.Content,
            Images = post.Images.ToList(),
            UserName = post.User.UserName,
            ProfilePicture = post.User.ProfilePictureUrl ?? "/images/default-profile.png",
            IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == currentUserId),
            IsSavedByCurrentUser = post.SavedPosts.Any(sp => sp.UserId == currentUserId),
            LikeCount = post.Likes.Count,
            CommentCount = post.Comments.Count,
            Comments = post.Comments,
        };
    }

    public async Task<List<PostViewModel>> GetPostViewModel()
    {
        var posts = await _postRepository.GetAllPostsAsync();
        
        var currentUserId = _userManager.GetUserId(User);
        
        return posts.Select(p => new PostViewModel
        {
            PostId = p.PostId,
            Content = p.Content,
            Images = p.Images.ToList(),
            UserName = p.User.UserName,
            ProfilePicture = p.User.ProfilePictureUrl ?? "/images/default-profile.png",
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
            IsSavedByCurrentUser = p.SavedPosts.Any(sp => sp.UserId == currentUserId),
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count,
            Comments = p.Comments,
        }).ToList();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult ToggleLike(string postId, bool isLike)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        // Retrieve the post
        var post = _postRepository.GetPostByIdAsync(postId).Result;
        if (post == null)
        {
            return NotFound();
        }

        if (isLike)
        {
            // Add like
            if (!post.Likes.Any(l => l.UserId == userId))
            {
                _postRepository.AddLikeAsync(postId, userId);
            }
        }
        else
        {
                _postRepository.RemoveLikeAsync(postId, userId);
        }

        // Prepare the updated model
        var model = GetPostViewModelById(postId).Result;

        return PartialView("_LikeSavePartial", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult ToggleSave(string postId, bool isSave)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        // Retrieve the post
        var post = _postRepository.GetPostByIdAsync(postId).Result;
        if (post == null)
        {
            return NotFound();
        }

        if (isSave)
        {
            // Add save
            if (!post.SavedPosts.Any(sp => sp.UserId == userId))
            {
                _postRepository.AddSavedPost(postId, userId);
            }
        }
        else
        {
                _postRepository.RemoveSavedPost(postId, userId);
        }

        // Prepare the updated model
        var model = GetPostViewModelById(postId).Result;

        return PartialView("_LikeSavePartial", model);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult AddComment(string postId, string content)
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
        }

        if (!ModelState.IsValid)
        {
            // Return the current comments partial with validation errors
            var postViewModel = GetPostViewModelById(postId).Result;
            return PartialView("_CommentsPartial", postViewModel);
        }

        // Add the comment
        var comment = new Comment(postId, userId, content);
        _postRepository.AddCommentAsync(comment);

        // Retrieve updated comments
        var postViewModelUpdated = GetPostViewModelById(postId).Result;

        return PartialView("_CommentsPartial", postViewModelUpdated);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}