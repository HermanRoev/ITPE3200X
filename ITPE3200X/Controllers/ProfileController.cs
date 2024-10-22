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
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProfileController(UserManager<ApplicationUser> userManager, IUserRepository userRepository, IPostRepository postRepository, IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _postRepository = postRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: Profile
    public async Task<IActionResult> Profile(string username)
    {
        var currentUserId = _userManager.GetUserId(User);
        var userByName = _userManager.Users.FirstOrDefault(u => u.UserName == username);
        var userId = userByName?.Id;
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
            IsCurrentUserProfile = userId == currentUserId,
            IsFollowing = await _userRepository.IsFollowingAsync(currentUserId, userId)
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(string Content, List<IFormFile> ImageFiles)
    {
        if (string.IsNullOrWhiteSpace(Content))
        {
            ModelState.AddModelError("Content", "Content is required.");
        }

        if (!ModelState.IsValid)
        {
            return View();
        }

        var userId = _userManager.GetUserId(User);
        
        var username = _userManager.GetUserName(User);

        var post = new Post(userId, Content);

        // Handle image files
        if (ImageFiles.Count > 0)
        {
            foreach (var imageFile in ImageFiles)
            {
                if (imageFile.Length > 0)
                {
                    // Validate the image file (optional but recommended)
                    if (!IsImageFile(imageFile))
                    {
                        ModelState.AddModelError("ImageFiles", "One or more files are not valid images.");
                        return View();
                    }

                    // Generate a unique file name
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                    var uploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    var filePath = Path.Combine(uploads, fileName);

                    // Ensure the uploads directory exists
                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    // Save the image to the server
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    // Create PostImage entity and add to the post
                    var imageEntity = new PostImage(post.PostId, $"/uploads/{fileName}");
                    post.Images.Add(imageEntity);
                }
            }
        }
        else
        {
            ModelState.AddModelError("ImageFiles", "At least one image is required.");
            return View();
        }

        // Save the post to the database
        await _postRepository.AddPostAsync(post);

        return RedirectToAction("Profile", new { username });
    }

    private bool IsImageFile(IFormFile file)
    {
        var permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || !permittedExtensions.Contains(extension))
        {
            return false;
        }

        return true;
    }
    
    public async Task<IActionResult> Follow(string username)
    {
        var user = _userManager.Users.FirstOrDefault(u => u.UserName == username);
        var userId = user?.Id;
        var currentUserId = _userManager.GetUserId(User);
        await _userRepository.AddFollowerAsync(currentUserId, userId);
        return RedirectToAction("Profile", new { username });
    }
    
    public async Task<IActionResult> Unfollow(string username)
    {
        var user = _userManager.Users.FirstOrDefault(u => u.UserName == username);
        var userId = user?.Id;
        var currentUserId = _userManager.GetUserId(User);
        await _userRepository.RemoveFollowerAsync(currentUserId, userId);
        return RedirectToAction("Profile", new { username });
    }
}
