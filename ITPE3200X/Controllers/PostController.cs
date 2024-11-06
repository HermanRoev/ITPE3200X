using ITPE3200X.DAL.Repositories;
using ITPE3200X.Models;
using ITPE3200X.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


namespace ITPE3200X.Controllers;

public class PostController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly IPostRepository _postRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<HomeController> _logger;

    public PostController(
        ILogger<HomeController> logger,
        UserManager<ApplicationUser> userManager,
        IUserRepository userRepository,
        IPostRepository postRepository,
        IWebHostEnvironment webHostEnvironment
    )
    {
        _logger = logger;
        _userManager = userManager;
        _userRepository = userRepository;
        _postRepository = postRepository;
        _webHostEnvironment = webHostEnvironment;
    }
    
    public IActionResult CreatePost()
        {
            return View();
        }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(string content, List<IFormFile> imageFiles)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError("Content", "Content is required.");
        }

        if (!ModelState.IsValid)
        {
            ModelState.AddModelError("", "Invalid model.");
            return View();
        }

        var userId = _userManager.GetUserId(User);
        
        var post = new Post(userId!, content);

        // Handle image files
        if (imageFiles.Count > 0)
        {
            foreach (var imageFile in imageFiles)
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

        return RedirectToAction("Profile", "Profile", new { username = _userManager.GetUserName(User) });
    }

    private bool IsImageFile(IFormFile file)
    {
        var permittedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || !permittedExtensions.Contains(extension))
        {
            return false;
        }

        return true;
    }
    
    public async Task<PostViewModel> GetPostViewModelById(String postId, bool homefeed)
    {
        var post = await _postRepository.GetPostByIdAsync(postId);
        
        var currentUserId = _userManager.GetUserId(User);

        return new PostViewModel
        {
            PostId = post.PostId,
            Content = post.Content,
            Images = post.Images.ToList(),
            UserName = post.User.UserName!,
            ProfilePicture = post.User.ProfilePictureUrl ?? "/images/default-profile.png",
            IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == currentUserId),
            IsSavedByCurrentUser = post.SavedPosts.Any(sp => sp.UserId == currentUserId),
            IsOwnedByCurrentUser = post.UserId == currentUserId,
            HomeFeed = homefeed,
            LikeCount = post.Likes.Count,
            CommentCount = post.Comments.Count,
            Comments = post.Comments
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
        };
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
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult ToggleLike(string postId, bool isLike, bool homefeed)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        // Retrieve the post
        var post = _postRepository.GetPostByIdAsync(postId).Result;

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
        var model = GetPostViewModelById(postId, homefeed).Result;

        return PartialView("_PostPartial", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult ToggleSave(string postId, bool isSave, bool homefeed)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        // Retrieve the post
        var post = _postRepository.GetPostByIdAsync(postId).Result;

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
        var model = GetPostViewModelById(postId, homefeed).Result;

        return PartialView("_PostPartial", model);
    }
    
    [HttpGet]
    public async Task<IActionResult> EditPost(string postId)
    {
        var userId = _userManager.GetUserId(User);

        var post = await _postRepository.GetPostByIdAsync(postId);

        if (post.UserId != userId)
        {
            return Unauthorized();
        }

        var model = new EditPostViewModel
        {
            PostId = post.PostId,
            Content = post.Content,
            Images = post.Images.ToList()
        };

        return View(model);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(string postId, string content, List<IFormFile>? imageFiles)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            Console.WriteLine("Model state is not valid");
            return RedirectToAction("EditPost", new { postId });
        }

        var postToUpdate = await _postRepository.GetPostByIdAsync(postId);

        if (postToUpdate.UserId != userId)
        {
            return NotFound();
        }

        // Update the content
        postToUpdate.Content = content;

        // Prepare a list to hold images to delete
        List<PostImage> imagesToDelete = new List<PostImage>();
        List<PostImage> imagesToAdd = new List<PostImage>();
        
        // Handle image replacement if there are new images
        if (imageFiles != null && imageFiles.Any())
        {
            // Delete existing images
            foreach (var image in postToUpdate.Images.ToList())
            {
                DeleteImageFile(image.ImageUrl);
                imagesToDelete.Add(image);
            }

            // Add new images
            foreach (var imageFile in imageFiles)
            {
                if (imageFile.Length > 0)
                {
                    // Validate the image file
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
                    var imageEntity = new PostImage(postToUpdate.PostId, $"/uploads/{fileName}");
                    imagesToAdd.Add(imageEntity);
                }
            }
        }

        // Save changes to the database
        await _postRepository.UpdatePostAsync(postToUpdate, imagesToDelete, imagesToAdd);

        return RedirectToAction("Profile", "Profile", new { username = _userManager.GetUserName(User) });
    }
        
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult AddComment(string postId, string content, bool homefeed)
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
            var postViewModel = GetPostViewModelById(postId, homefeed).Result;
            return PartialView("_PostPartial", postViewModel);
        }

        // Add the comment
        var comment = new Comment(postId, userId, content);
        _postRepository.AddCommentAsync(comment);

        // Retrieve updated comments
        var postViewModelUpdated = GetPostViewModelById(postId, homefeed).Result;

        return PartialView("_PostPartial", postViewModelUpdated);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult DeleteComment(string postId, string commentId, bool homefeed)
    {
        var userId = _userManager.GetUserId(User);
        
        _postRepository.DeleteCommentAsync(commentId, userId!);
        
        // Retrieve updated comments
        var postViewModelUpdated = GetPostViewModelById(postId, homefeed).Result;

        return PartialView("_PostPartial", postViewModelUpdated);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditComment(string postId, string commentId, string content, bool homefeed)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError("Content", "Content is required.");
        }

        if (!ModelState.IsValid)
        {
            // Handle invalid model state, possibly return an error response
            return BadRequest(ModelState);
        }

        try
        {
            await _postRepository.EditCommentAsync(commentId, userId!, content);

            // Retrieve updated post view model
            var postViewModelUpdated = GetPostViewModelById(postId, homefeed).Result;

            return PartialView("_PostPartial", postViewModelUpdated);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(string postId, bool homefeed)
    {
        var userId = _userManager.GetUserId(User);

        // Retrieve the post with images
        var post = await _postRepository.GetPostByIdAsync(postId);

        if (post.UserId != userId)
        {
            return Unauthorized();
        }

        // Delete image files from the file system
        foreach (var image in post.Images)
        {
            DeleteImageFile(image.ImageUrl);
        }

        // Delete the post (and associated images) from the database
        await _postRepository.DeletePostAsync(postId, userId);

        if (homefeed)
        {
            return RedirectToAction("Index", "Home");
        }
        return RedirectToAction("Profile", "Profile");
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
    
    public IActionResult SavedPosts()
    {
        var userId = _userManager.GetUserId(User);
        
        var savedPosts = _userRepository.GetSavedPostsByUserIdAsync(userId!).Result;
        
        var postViewModels = savedPosts.Select(p => new PostViewModel
        {
            PostId = p.PostId,
            Content = p.Content,
            Images = p.Images.ToList(),
            UserName = p.User.UserName!,
            ProfilePicture = p.User.ProfilePictureUrl!,
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == userId),
            IsSavedByCurrentUser = p.SavedPosts.Any(sp => sp.UserId == userId),
            IsOwnedByCurrentUser = p.UserId == userId,
            HomeFeed = false,
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count,
            Comments = p.Comments
                .OrderBy(c => c.CreatedAt) // Order comments by CreatedAt (ascending)
                // .OrderByDescending(c => c.CreatedAt) // Use this line instead for descending order
                .Select(c => new CommentViewModel
                {
                    IsCreatedByCurrentUser = c.UserId == userId,
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
}