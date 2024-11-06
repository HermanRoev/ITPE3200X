using System.Diagnostics;
using ITPE3200X.DAL.Repositories;
using Microsoft.AspNetCore.Mvc;
using ITPE3200X.Models;
using Microsoft.AspNetCore.Identity;
using ITPE3200X.ViewModels;

namespace ITPE3200X.Controllers;

public class HomeController : Controller
{
    private readonly IPostRepository _postRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        IPostRepository postRepository,
        UserManager<ApplicationUser> userManager
        )
    {
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
            UserName = p.User.UserName!,
            ProfilePicture = p.User.ProfilePictureUrl!,
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
            IsSavedByCurrentUser = p.SavedPosts.Any(sp => sp.UserId == currentUserId),
            IsOwnedByCurrentUser = p.UserId == currentUserId,
            HomeFeed = true,
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count,
            Comments = p.Comments
                .OrderBy(c => c.CreatedAt) // Order comments by CreatedAt (ascending)
                // .OrderByDescending(c => c.CreatedAt) // Use this line instead for descending order
                .Select(c => new CommentViewModel
                {
                    IsCreatedByCurrentUser = c.UserId == currentUserId,
                    CommentId = c.CommentId,
                    UserName = c.User.UserName!,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    TimeSincePosted = CalculateTimeSincePosted(c.CreatedAt)
                })
                .ToList()
        }).ToList();
        
        return View(postViewModels);
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}