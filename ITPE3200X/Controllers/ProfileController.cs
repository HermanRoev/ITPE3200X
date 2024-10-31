using Microsoft.AspNetCore.Mvc;
using ITPE3200X.Models;
using ITPE3200X.DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using ITPE3200X.ViewModels;


namespace ITPE3200X.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly IPostRepository _postRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            IUserRepository userRepository,
            IPostRepository postRepository,
            IWebHostEnvironment webHostEnvironment
        )
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _postRepository = postRepository;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Profile
        public async Task<IActionResult> Profile(string? username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                var userExists = _userManager.Users.Any(u => u.UserName == username);
                if (!userExists)
                {
                    return NotFound();
                }
            }
            else
            {
                username = _userManager.GetUserName(User);
            }
            
            var currentUserId = _userManager.GetUserId(User);
            
            var user = _userManager.Users.FirstOrDefault(u => u.UserName == username);
            
            var posts = _postRepository.GetPostsByUserAsync(user.Id).Result;
            
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
                HomeFeed = false,
                LikeCount = p.Likes.Count,
                CommentCount = p.Comments.Count,
                Comments = p.Comments
                    .OrderBy(c => c.CreatedAt) // Order comments by CreatedAt (ascending)
                    // .OrderByDescending(c => c.CreatedAt) // Use this line instead for descending order
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

            var profile = new ProfileViewModel
            {
                User = user,
                Posts = postViewModels,
                IsCurrentUserProfile = user.Id == currentUserId,
                IsFollowing = await _userRepository.IsFollowingAsync(currentUserId, user.Id)
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
            var user = _userManager.GetUserAsync(User).Result;

            var model = new EditProfileViewModel
            {
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Invalid model.");
                return View(model);
            }

            var user = _userManager.GetUserAsync(User).Result;

            // Handle Profile Picture Upload
            if (model.ImageFile != null)
            {
                // Validate the image file (optional but recommended)
                if (!IsImageFile(model.ImageFile))
                {
                    ModelState.AddModelError("ImageFile", "The file is not a valid image.");
                    return View(model);
                }

                // Generate a unique file name
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ImageFile.FileName)}";
                var uploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profile_pictures");
                var filePath = Path.Combine(uploads, fileName);

                // Ensure the uploads directory exists
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads);
                }

                // Save the image to the server
                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }

                // Delete the old profile picture if it exists and is not the default
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    DeleteImageFile(user.ProfilePictureUrl);
                }

                // Update the user's ProfilePictureUrl
                user.ProfilePictureUrl = $"/uploads/profile_pictures/{fileName}";
            }

            // Update other user properties
            user.Bio = model.Bio;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Could not update profile.");
                return View(model);
            }

            return RedirectToAction("Profile", new { username = user.UserName });
        }

        private void DeleteImageFile(string imageUrl)
        {
            try
            {
                var wwwRootPath = _webHostEnvironment.WebRootPath;
                var filePath = Path.Combine(wwwRootPath,
                    imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                // Log the exception (you can inject a logger if needed)
                Console.WriteLine($"Error deleting image file: {ex.Message}");
            }
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
            
            var post = new Post(userId, content);

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

            return RedirectToAction("Profile", new { username = _userManager.GetUserName(User) });
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
}