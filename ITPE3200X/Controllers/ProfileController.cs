using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITPE3200X.Models;
using System.Linq;
using System.Threading.Tasks;
using ITPE3200X.DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using ITPE3200X.ViewModels;
using Microsoft.AspNetCore.Hosting;
using System.IO;

public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly IPostRepository _postRepository;

    public ProfileController(UserManager<ApplicationUser> userManager, IUserRepository userRepository, IPostRepository postRepository)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _postRepository = postRepository;
    }

    // GET: Profile
    public async Task<IActionResult> Profile()
    {
        var userId = _userManager.GetUserId(User);
        var user = _userRepository.GetUserdataById(userId).Result;
        
        var posts = _postRepository.GetPostsByUserAsync(userId).Result;
        var postViewModels = posts.Select(p => new PostViewModel
        {
            PostId = p.PostId,
            Content = p.Content,
            Images = p.Images.ToList(),
            UserName = p.User.UserName,
            ProfilePicture = p.User.ProfilePictureUrl,
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == userId),
            IsSavedByCurrentUser = p.SavedPosts.Any(sp => sp.UserId == userId),
            IsOwnedByCurrentUser = p.UserId == userId,
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count,
            Comments = p.Comments
                .OrderBy(c => c.CreatedAt) // Order comments by CreatedAt (ascending)
                // .OrderByDescending(c => c.CreatedAt) // Use this line instead for descending order
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
        
        var profile = new ProfileViewModel
        {
            User = user,
            Posts = postViewModels,
        };
        
        return View(profile);
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

    // GET: EditProfile
    public async Task<IActionResult> Edit()
    {
        var userId = _userManager.GetUserId(User);
        var user = _userRepository.GetUserdataById(userId).Result;
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ApplicationUser model)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Profile");
        }

        user.ProfilePictureUrl = model.ProfilePictureUrl;
        user.Bio = model.Bio;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Kunne ikke oppdatere profil.");
            return View(model);
        }

        return RedirectToAction("Profile");
    }

    public IActionResult CreatePost()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> CreatePost(PostViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        var user = _userRepository.GetUserdataById(userId).Result;
        var post = new Post(userId, model.Content);

        await _postRepository.AddPostAsync(post);
        
        return RedirectToAction("Profile");
    }
}







