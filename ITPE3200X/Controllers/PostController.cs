using ITPE3200X.DAL.Repositories;
using ITPE3200X.Models;
using ITPE3200X.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ITPE3200X.Controllers;

public class PostController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPostRepository _postRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<PostController> _logger;

    // Constructor injecting dependencies
    public PostController(
        ILogger<PostController> logger,
        UserManager<ApplicationUser> userManager,
        IPostRepository postRepository,
        IWebHostEnvironment webHostEnvironment
    )
    {
        _logger = logger;
        _userManager = userManager;
        _postRepository = postRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    // Returns the create post view
    [Authorize]
    [HttpGet]
    public IActionResult CreatePost()
    {
        return View();
    }

    // Creates a new post, saves it to the database, and redirects to the user's profile
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePost(string content, List<IFormFile> imageFiles)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError("Content", "Content is required.");
            _logger.LogWarning("[PostController][CreatePost] Content is required.");
        }

        if (!imageFiles.Any())
        {
            ModelState.AddModelError("ImageFiles", "At least one image is required.");
            _logger.LogWarning("[PostController][CreatePost] At least one image is required.");
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[PostController][CreatePost] Invalid model state.");
            return View();
        }

        var userId = _userManager.GetUserId(User);

        var post = new Post(userId!, content);

        // Handle image files
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
                var imageEntity = new PostImage(post.PostId, $"/uploads/{fileName}");
                post.Images.Add(imageEntity);
            }
        }

        // Save the post to the database
        var result = await _postRepository.AddPostAsync(post);
        
        if(!result)
        {
            _logger.LogError("[PostController][CreatePost] Error adding post to database.");
            return View();
        }
        
        return RedirectToAction("Profile", "Profile", new { username = _userManager.GetUserName(User) });
    }

    // Checks if a file is a valid image file based on its extension
    private bool IsImageFile(IFormFile file)
    {
        var permittedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return !string.IsNullOrEmpty(extension) && permittedExtensions.Contains(extension);
    }

    // Returns a post view model by post id, used to update the view after actions
    private async Task<PostViewModel?> GetPostViewModelById(string postId, bool homefeed)
    {
        var post = await _postRepository.GetPostByIdAsync(postId);
        
        if (post == null)
        {
            _logger.LogWarning("[PostController][GetPostViewModelById] Post not found: {PostId}", postId);
            return null;
        }

        var currentUserId = _userManager.GetUserId(User);

        return new PostViewModel
        {
            PostId = post.PostId,
            Content = post.Content,
            Images = post.Images.ToList(),
            UserName = post.User.UserName,
            ProfilePicture = post.User.ProfilePictureUrl,
            IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == currentUserId),
            IsSavedByCurrentUser = post.SavedPosts.Any(sp => sp.UserId == currentUserId),
            IsOwnedByCurrentUser = post.UserId == currentUserId,
            HomeFeed = homefeed,
            LikeCount = post.Likes.Count,
            CommentCount = post.Comments.Count,
            Comments = post.Comments
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
        };
    }

    // Calculates the time since a comment was created
    private string CalculateTimeSincePosted(DateTime createdAt)
    {
        var currentTime = DateTime.UtcNow;

        if (createdAt > currentTime)
        {
            _logger.LogWarning("[PostController][CalculateTimeSincePosted] CreatedAt timestamp is in the future: {CreatedAt}", createdAt);
            createdAt = currentTime;
        }

        var timeSpan = currentTime - createdAt;

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

    // Toggles the like status of a post
    [Authorize]
    [HttpPost]
    public async Task<ActionResult> ToggleLike(string postId, bool homefeed)
    {
        // Get the user id
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("[PostController][ToggleLike] User not authenticated.");
            return Unauthorized();
        }

        // Retrieve the post
        var post = await _postRepository.GetPostByIdAsync(postId);
        
        if(post == null)
        {
            _logger.LogWarning("[PostController][ToggleLike] Post not found: {PostId}", postId);
            return NotFound();
        }
        
        // Add like
        if (!post.Likes.Any(l => l.UserId == userId))
        {
            await _postRepository.AddLikeAsync(postId, userId);
        }
        else
        {
            // Remove like
            await _postRepository.RemoveLikeAsync(postId, userId);
        }

        // Prepare the updated model
        var model = await GetPostViewModelById(postId, homefeed);

        return PartialView("_PostPartial", model);
    }

    // Toggles the save status of a post
    [Authorize]
    [HttpPost]
    public async Task<ActionResult> ToggleSave(string postId, bool homefeed)
    {
        // Get the user id
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("[PostController][ToggleSave] User not authenticated.");
            return Unauthorized();
        }

        // Retrieve the post
        var post = await _postRepository.GetPostByIdAsync(postId);
        
        if(post == null)
        {
            _logger.LogWarning("[PostController][ToggleSave] Post not found: {PostId}", postId);
            return NotFound();
        }

        // Add save
        if (!post.SavedPosts.Any(sp => sp.UserId == userId))
        {
            await _postRepository.AddSavedPostAsync(postId, userId);
        }
        else
        {
            // Remove save
            await _postRepository.RemoveSavedPostAsync(postId, userId);
        }

        // Prepare the updated model
        var model = await GetPostViewModelById(postId, homefeed);

        return PartialView("_PostPartial", model);
    }

    // Returns the edit post view
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditPost(string postId)
    {
        var userId = _userManager.GetUserId(User);
        
        // Check if the user is authenticated
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("[PostController][EditPost] User not authenticated.");
            return Unauthorized();
        }
        
        // Retrieve the post to edit
        var post = await _postRepository.GetPostByIdAsync(postId);
        
        if(post == null)
        {
            _logger.LogWarning("[PostController][EditPost] Post not found: {PostId}", postId);
            return NotFound();
        }
        
        // Check if the current user is the owner of the post
        if (post.UserId != userId)
        {
            _logger.LogWarning("[PostController][EditPost] User is not owner of the post: {PostId}", postId);
            return Forbid();
        }

        // Create the view model
        var model = new EditPostViewModel
        {
            PostId = post.PostId,
            Content = post.Content,
            Images = post.Images.ToList()
        };

        return View(model);
    }

    // Updates a post, saves it to the database, and redirects to the user's profile
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> EditPost(string postId, string content, List<IFormFile>? imageFiles)
    {
        var userId = _userManager.GetUserId(User);
        
        // Check if the user is authenticated
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("[PostController][EditPost] User not authenticated.");
            return Unauthorized();
        }
        
        // Validate the content
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("[PostController][EditPost] Content is required.");
            ModelState.AddModelError("Content", "Content is required.");
        }
        
        // Check if the model state is valid
        if (!ModelState.IsValid)
        {
            return RedirectToAction("EditPost", new { postId });
        }

        var postToUpdate = await _postRepository.GetPostByIdAsync(postId);
        
        // Check if the post exists
        if(postToUpdate == null)
        {
            return NotFound();
        }

        // Update the content
        postToUpdate.Content = content;

        // Prepare lists to hold images to delete and add
        var imagesToDelete = new List<PostImage>();
        var imagesToAdd = new List<PostImage>();

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
                        _logger.LogWarning("[PostController][EditPost] One or more files are not valid images.");
                        ModelState.AddModelError("ImageFiles", "One or more files are not valid images.");
                        return RedirectToAction("EditPost", new { postId });
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

                    // Create PostImage entity and add to the list
                    var imageEntity = new PostImage(postToUpdate.PostId, $"/uploads/{fileName}");
                    imagesToAdd.Add(imageEntity);
                }
            }
        }

        // Save changes to the database
        var result = await _postRepository.UpdatePostAsync(postToUpdate, imagesToDelete, imagesToAdd);

        if(!result)
        {
            _logger.LogError("[PostController][EditPost] Error updating post in database.");
            return RedirectToAction("EditPost", new { postId });
        }
        
        return RedirectToAction("Profile", "Profile", new { username = _userManager.GetUserName(User) });
    }

    // Adds a comment to a post
    [Authorize]
    [HttpPost]
    public async Task<ActionResult> AddComment(string postId, string content, bool homefeed)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("[PostController][AddComment] User not authenticated.");
            return Unauthorized();
        }
        
        // Validate the content
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("[PostController][AddComment] Comment cannot be empty.");
            ModelState.AddModelError("Content", "Comment cannot be empty.");
        }
        
        // Check if the model state is valid
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[PostController][AddComment] Invalid model state.");
            var postViewModel = await GetPostViewModelById(postId, homefeed);
            return PartialView("_PostPartial", postViewModel);
        }

        var comment = new Comment(postId, userId, content);
        var result = await _postRepository.AddCommentAsync(comment);
        
        // Check if the comment was added successfully
        if(!result)
        {
            _logger.LogError("[PostController][AddComment] Error adding comment to database.");
            return BadRequest();
        }

        var postViewModelUpdated = await GetPostViewModelById(postId, homefeed);

        return PartialView("_PostPartial", postViewModelUpdated);
    }

    // Deletes a comment from a post
    [Authorize]
    [HttpPost]
    public async Task<ActionResult> DeleteComment(string postId, string commentId, bool homefeed)
    {
        var userId = _userManager.GetUserId(User);
    
        // Check if the user is authenticated
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("[PostController][DeleteComment] User not authenticated.");
            return Unauthorized();
        }

        var result = await _postRepository.DeleteCommentAsync(commentId, userId);
        
        // Check if the comment was deleted successfully
        if(!result)
        {
            _logger.LogError("[PostController][DeleteComment] Error deleting comment from database.");
            return BadRequest();
        }

        var postViewModelUpdated = await GetPostViewModelById(postId, homefeed);

        return PartialView("_PostPartial", postViewModelUpdated);
    }

    // Edits a comment on a post
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> EditComment(string postId, string commentId, string content, bool homefeed)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("[PostController][EditComment] User not authenticated.");
            return Unauthorized();
        }
        
        // Validate the content
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("[PostController][EditComment] Content is required.");
            ModelState.AddModelError("Content", "Content is required.");
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[PostController][EditComment] Invalid model state.");
            return BadRequest(ModelState);
        }

        var result = await _postRepository.EditCommentAsync(commentId, userId, content);
        
        // Check if the comment was edited successfully
        if(!result)
        {
            _logger.LogError("[PostController][EditComment] Error editing comment in database.");
            return BadRequest();
        }

        var postViewModelUpdated = await GetPostViewModelById(postId, homefeed);

        return PartialView("_PostPartial", postViewModelUpdated);
    }
    

    // Deletes a post and associated images
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DeletePost(string postId, bool homefeed)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("[PostController][DeletePost] User not authenticated.");
            return Unauthorized();
        }

        // Retrieve the post with images
        var post = await _postRepository.GetPostByIdAsync(postId);
        
        if(post == null)
        {
            _logger.LogWarning("[PostController][DeletePost] Post not found: {PostId}", postId);
            return NotFound();
        }
        
        // Check if the current user is the owner of the post
        if (post.UserId != userId)
        {
            _logger.LogWarning("[PostController][DeletePost] User is not owner of the post: {PostId}", postId);
            return Forbid();
        }

        // Delete image files from the file system
        foreach (var image in post.Images)
        {
            DeleteImageFile(image.ImageUrl);
        }

        // Delete the post from the database
        var result = await _postRepository.DeletePostAsync(postId, userId);
        
        if(!result)
        {
            _logger.LogError("[PostController][DeletePost] Error deleting post from database.");
            return BadRequest();
        }
        
        // Redirect to the user's profile or home feed
        if (homefeed)
        {
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Profile", "Profile", new { username = _userManager.GetUserName(User) });
    }

    // Deletes an image file from the file system
    public void DeleteImageFile(string imageUrl)
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
            _logger.LogError(ex, "[PostController][DeleteImageFile] Error deleting image file: {ImageUrl}", imageUrl);
        }
    }

    // Returns the saved posts view
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> SavedPosts()
    {
        var userId = _userManager.GetUserId(User);
        
        if (string.IsNullOrEmpty(userId))
        {
            return NotFound();
        }

        var savedPosts = await _postRepository.GetSavedPostsByUserIdAsync(userId);
        
        // Check if the user has any saved posts
        if(savedPosts == null)
        {
            return NotFound();
        }
        
        // Create a list of PostViewModels for the saved posts
        var postViewModels = savedPosts.Select(p => new PostViewModel
        {
            PostId = p.PostId,
            Content = p.Content,
            Images = p.Images.ToList(),
            UserName = p.User.UserName,
            ProfilePicture = p.User.ProfilePictureUrl,
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == userId),
            IsSavedByCurrentUser = true, // Saved posts are always saved by the current user
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