using System.Security.Claims;
using ITPE3200X.Controllers;
using ITPE3200X.DAL.Repositories;
using ITPE3200X.Models;
using ITPE3200X.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ITPE3200X.Tests.Controllers;

public class PostControllerTest
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private readonly Mock<ILogger<PostController>> _mockLogger;
    private readonly PostController _controller;

    public PostControllerTest()
    {
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!
        );
        _mockPostRepository = new Mock<IPostRepository>();
        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<PostController>>();

        // Initialize the controller with mocked dependencies
        _controller = new PostController(
            _mockLogger.Object,
            _mockUserManager.Object,
            _mockPostRepository.Object,
            _mockWebHostEnvironment.Object
        );

        // Set up the mock environment root path for file handling
        _mockWebHostEnvironment.Setup(e => e.WebRootPath).Returns("wwwroot");
    }

//CREATE POST TESTS
    //positive test for creating a post
    [Fact]
    public Task CreatePost_ReturnsViewWithPost()
    {
        // Act
        var result = _controller.CreatePost() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ViewResult>(result);
        return Task.CompletedTask;
    }
    
    //positive test for creating a post with valid content and image upload
    [Fact]
    public async Task CreatePost_ValidPostWithImage()
    {
        // Arrange
        var userId = "testUserId";
        var content = "Sample content";
        var mockImageFile = new Mock<IFormFile>();
        var imageFiles = new List<IFormFile> { mockImageFile.Object };

        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(r => r.AddPostAsync(It.IsAny<Post>())).ReturnsAsync(true);

        // Act
        var result = await _controller.CreatePost(content, imageFiles) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Profile", result.ActionName);
    }
    
    //negative test for creating a post with invalid content and no image 
    [Fact]
    public async Task CreatePost_InvalidPostNoImage()
    {
        var content = "";
        var imageFiles = new List<IFormFile>(); // No images

        // Act
        var result = await _controller.CreatePost(content, imageFiles) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey("Content"));
        Assert.True(_controller.ModelState.ContainsKey("ImageFiles"));
    }
    
//TOGGLE LIKE METHOD 
    //positive test for toggling a like on a post
    [Fact]
    public async Task ToggleLike_ValidPost()
    {
        //Arrange
        var postId = "testPostId";
        var homefeed = true;
        var userId = "testUserId";

        // Mock post data
        var post = new Post(userId, "Test content")
        {
            PostId = postId,
            UserId = userId,
            Likes = new List<Like>(), // Initially no likes
            Comments = new List<Comment>(),
            Images = new List<PostImage>()
        };

        // Mock methods
        _mockPostRepository.Setup(pr => pr.GetPostByIdAsync(postId)).ReturnsAsync(post);
        _mockPostRepository.Setup(pr => pr.AddLikeAsync(postId, userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ToggleLike(postId, homefeed);

        // Assert
        Assert.NotNull(result); 
    }
    
    //negative test for toggling a like on a post that does not exist
    [Fact]
    public async Task ToggleLike_InvalidPost()
    {
        // Arrange
        var postId = "testPostId";
        var userId = "testUserId";

        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(r => r.AddLikeAsync(postId, userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ToggleLike(postId, false) as NotFoundResult;

        // Assert
        var viewResult = Assert.IsType<NotFoundResult>(result); // Checks if the result is a NotFoundResult
        Assert.NotNull(viewResult); // Ensure NotFoundResult is not null 
    }
    
    //negative test for toggling like when unauthorized user tries to like a post
    [Fact]
    public async Task ToggleLike_UnauthorizedUser()
    {
        // Arrange
        var postId = "testPostId";
        var homefeed = true;

        // Simulate unauthenticated user by providing an empty ClaimsIdentity
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                // Empty identity ensures User.Identity is not null
                User = new ClaimsPrincipal(new ClaimsIdentity()) 
            }
        };

        // Mock GetUserId to return null
        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string?)null);

        // Act
        var result = await _controller.ToggleLike(postId, homefeed);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

//TOGGLE SAVE METHOD 
    //positive test for toggling a save on a post
    
    //negative test for toggling a save on a post that does not exist
    [Fact]
    public async Task ToggleSave_InvalidPost()
    {
        // Arrange
        var postId = "testPostId";
        var userId = "testUserId";

        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(r => r.AddSavedPostAsync(postId, userId)).Returns(Task.CompletedTask);
        
        // Act
        var result = await _controller.ToggleSave(postId, false) as NotFoundResult;

        // Assert
        var viewResult = Assert.IsType<NotFoundResult>(result); // Checks if the result is a NotFoundResult
        Assert.NotNull(viewResult); // Ensure NotFoundResult is not null 
    }
    
    //negative test for toggling save when unauthorized user tries to save a post
    [Fact]
    public async Task ToggleSave_UnauthorizedUser()
    {
        // Arrange
        var postId = "testPostId";
        var homefeed = true;

        // Simulate unauthenticated user by providing an empty ClaimsIdentity
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                // Empty identity ensures User.Identity is not null
                User = new ClaimsPrincipal(new ClaimsIdentity()) 
            }
        };

        // Mock GetUserId to return null
        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string?)null);

        // Act
        var result = await _controller.ToggleSave(postId, homefeed);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }
    
//EDIT POST VIEW METHOD 
    //positive test for editing a post and showing EditPostView
    [Fact]
    public async Task EditPostView_RetunsView()
    {
        //arrange
        var userId = "testUserId";
        var postId = "testPostId";
        
        // Mock post data
        var post = new Post(userId, "Test content")
        {
            PostId = postId,
            UserId = userId,
            Content = "Old content",
            Images = new List<PostImage>()
        };

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(pr => pr.GetPostByIdAsync(postId)).ReturnsAsync(post);
        
        //act
        var result = await _controller.EditPost(postId) as ViewResult;
        
        //assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<EditPostViewModel>(viewResult.Model);
        Assert.Equal(post.Content, model.Content);
    }
    
    //negative test for showing EditPostView when post does not exist
    [Fact]
    public async Task EditPost_PostPostsNotExist()
    {
        // Arrange
        var userId = "testUserId";
        var postId = "testPostId";

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(pr => pr.GetPostByIdAsync(postId)).ReturnsAsync((Post?)null);

        // Act
        var result = await _controller.EditPost(postId) as NotFoundResult;

        // Assert
        Assert.NotNull(result);
    }
    
    //negative test for edit post view when unauthorized user tries to edit a post
    [Fact]
    public async Task EditPostView_UnauthorizedUser()
    {
        // Arrange
        var postId = "testPostId";

        // Simulate unauthenticated user by providing an empty ClaimsIdentity
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                // Empty identity ensures User.Identity is not null
                User = new ClaimsPrincipal(new ClaimsIdentity()) 
            }
        };

        // Mock GetUserId to return null
        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string?)null);

        // Act
        var result = await _controller.EditPost(postId);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }
    
//EDIT POST METHOD 
    //positive test for editing a post
    [Fact]
    public async Task EditPost_ValidPost()
    {
       //arrange 
       var userId = "testUserId";
       var postId = "testPostId";

       var post = new Post(userId, "Test content")
       {
           PostId = postId,
           UserId = userId,
           Content = "Old content",
           Images = new List<PostImage>()
       };

       _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
       _mockPostRepository.Setup(pr => pr.GetPostByIdAsync(postId)).ReturnsAsync(post);
       _mockPostRepository.Setup(pr => pr.UpdatePostAsync(It.IsAny<Post>(), It.IsAny<List<PostImage>>(), It.IsAny<List<PostImage>>()))
           .ReturnsAsync(true);
       
       //act 
       var result = await _controller.EditPost(postId, "new content", null);
       
       //assert 
       var redirectResult = Assert.IsType<RedirectToActionResult>(result);
       Assert.Equal("Profile", redirectResult.ActionName);
       Assert.Equal("Profile", redirectResult.ControllerName);
    }
    
    //negative test for editing a post is invalid 
    [Fact]
    public async Task EditPost_InvalidPost()
    {
       var userId = "testUserId";
       var postId = "testPostId";

       _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
       
       //act 
       var result = await _controller.EditPost(postId, "", null);
       
       //assert 
       var redirectResult = Assert.IsType<RedirectToActionResult>(result);
       Assert.Equal("EditPost", redirectResult.ActionName);
       Assert.Equal(postId, redirectResult.RouteValues!["postId"]);
    }
    
    //negative test for editing a post when repository fails to update post
    [Fact]
    public async Task EditPost_RepositoryFails_ReturnsRedirectToEdit()
    {
        //arrange
        var userId = "testUserId";
        var postId = "testPostId";
        
        var post = new Post(userId, "Test content")
        {
            PostId = postId,
            UserId = userId,
            Content = "Old content",
            Images = new List<PostImage>()
        };

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(pr => pr.GetPostByIdAsync(postId)).ReturnsAsync(post);
        _mockPostRepository.Setup(pr => pr.UpdatePostAsync(It.IsAny<Post>(), It.IsAny<List<PostImage>>(), It.IsAny<List<PostImage>>()))
            .ReturnsAsync(false);
        
        //act 
        var result = await _controller.EditPost(postId, "new content", null);
        
        //assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("EditPost", redirectResult.ActionName);
        Assert.Equal(postId, redirectResult.RouteValues?["postId"]);
    }
    
    //negative test for editing a post when unauthorized user tries to edit a post
    [Fact]
    public async Task EditPost_UnauthorizedUser()
    {
        // Arrange
        var postId = "testPostId";

        // Simulate unauthenticated user by providing an empty ClaimsIdentity
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                // Empty identity ensures User.Identity is not null
                User = new ClaimsPrincipal(new ClaimsIdentity()) 
            }
        };

        // Mock GetUserId to return null
        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string?)null);

        // Act
        var result = await _controller.EditPost(postId, "new content", null);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }
    
//ADD COMMENT METHOD 
    //positive test for add comment to a post
    [Fact]
    public async Task AddComment_ValidComment()
    {
        // Arrange
        var comment = new Comment("testUserId", "testPostId", "This is a valid comment.")
        {
            CommentId = Guid.NewGuid().ToString(),
        };

        _mockPostRepository.Setup(repo => repo.AddCommentAsync(comment)).ReturnsAsync(true);

        // Act
        var result = await _mockPostRepository.Object.AddCommentAsync(comment);

        // Assert
        Assert.True(result);
        _mockPostRepository.Verify(repo => repo.AddCommentAsync(comment), Times.Once);
    }
    
    //negative test for add comment when repository fails to add comment
    [Fact]
    public async Task AddComment_RepositoryFails_ReturnsFalse()
    {
        // Arrange
        var comment = new Comment("testUserId", "testPostId", "This is a valid comment.")
        {
            CommentId = Guid.NewGuid().ToString(),
        };

        _mockPostRepository.Setup(repo => repo.AddCommentAsync(comment)).ReturnsAsync(false);

        // Act
        var result = await _mockPostRepository.Object.AddCommentAsync(comment);

        // Assert
        Assert.False(result);
        _mockPostRepository.Verify(repo => repo.AddCommentAsync(comment), Times.Once);
    }
    
    //negative test for add comment with null input
    [Fact]
    public async Task AddComment_NullComment_ReturnsFalse()
    {
        //arrange 
        _mockPostRepository.Setup(repo => repo.AddCommentAsync(null!))
            .ThrowsAsync(new ArgumentNullException());
        
        //act & assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mockPostRepository.Object.AddCommentAsync(null!));
        _mockPostRepository.Verify(repo => repo.AddCommentAsync(null!), Times.Once);
    }

    //negative test for addComment when comment is empty 
    [Fact]
    public async Task AddCommentAsync_EmptyContent()
    {
        // Arrange
        var comment = new Comment("testUserId", "testPostId", "test content")
        {
            CommentId = Guid.NewGuid().ToString(),
        };

        _mockPostRepository.Setup(repo => repo.AddCommentAsync(comment)).ReturnsAsync(false);

        // Act
        var result = await _mockPostRepository.Object.AddCommentAsync(comment);

        // Assert
        Assert.False(result);
        _mockPostRepository.Verify(repo => repo.AddCommentAsync(comment), Times.Once);
    }
    
//DELETE COMMENT METHOD
    //positive for deleting a comment successfully
    [Fact]
    public async Task DeleteComment_ValidComment()
    {
        //arrange 
        var postId = "testPostId";
        var commentId = "testCommentId";
        var userId = "testUserId";
        var homefeed = true;
        
        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(repo => repo.DeleteCommentAsync(commentId, userId)).ReturnsAsync(true);
        
        //act
        var result = await _controller.DeleteComment(postId, commentId, homefeed);
        
        //assert
        Assert.IsType<PartialViewResult>(result);
    }
    
    //negative test for when user is not authenticated 
    [Fact]
    public async Task DeleteComment_UnauthenticatedUser()
    {
        // Arrange
        var postId = "testPostId";
        var commentId = "testCommentId";
        var homefeed = true;

        // Simulate unauthenticated user by providing an empty ClaimsIdentity
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                // Empty identity ensures User.Identity is not null
                User = new ClaimsPrincipal(new ClaimsIdentity()) 
            }
        };

        // Act
        var result = await _controller.DeleteComment(postId, commentId, homefeed);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

//EDIT COMMENT METHOD
    //positive test for editing a comment
    [Fact]
    public async Task EditComment_ValidComment()
    {
        // Arrange
        var postId = "testPostId";
        var commentId = "testCommentId";
        var content = "Updated comment content.";
        var homefeed = true;

        var comment = new Comment("testUserId", "testPostId", "Test content")
        {
            CommentId = commentId,
            UserId = "testUserId",
            Content = "Old content"
        };

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(comment.UserId);
        _mockPostRepository.Setup(repo => repo.EditCommentAsync(commentId, comment.UserId, content)).ReturnsAsync(true);

        // Act
        var result = await _controller.EditComment(postId, commentId, content, homefeed);

        // Assert
        Assert.IsType<PartialViewResult>(result);
    }
    
    //negative edit comment when user is not authenticated
    [Fact]
    public async Task EditComment_UnauthenticatedUser()
    {
        // Arrange
        var postId = "testPostId";
        var commentId = "testCommentId";
        var content = "Updated comment content.";
        var homefeed = true;

        // Simulate unauthenticated user by providing an empty ClaimsIdentity
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                // Empty identity ensures User.Identity is not null
                User = new ClaimsPrincipal(new ClaimsIdentity()) 
            }
        };

        // Act
        var result = await _controller.EditComment(postId, commentId, content, homefeed);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

//DELETE POST AND IMAGES METHOD
    //positive test for deleting a post and images redeirect to homefeed
    [Fact]
    public async Task DeletePost_ValidPostHomeFeed()
    {
        // Arrange
        var postId = "testPostId";
        var userId = "testUserId";
        var homefeed = true;

        var post = new Post("testUserId", "Test content")
        {
            Images = new List<PostImage>
            {
                new PostImage("testUserId", "/wwwroot/images/test3.jpg") 
                    { PostId = "testPostId" }
            }
        };

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(repo => repo.GetPostByIdAsync(postId)).ReturnsAsync(post);
        _mockPostRepository.Setup(repo => repo.DeletePostAsync(postId, userId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeletePost(postId, homefeed);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }
    
    //negative test for delete post when user is not authenticated
    [Fact]
    public async Task DeletePost_UnauthenticatedUser()
    {
        // Arrange
        var postId = "testPostId";
        var homefeed = true;

        // Simulate unauthenticated user by providing an empty ClaimsIdentity
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()) // Empty identity ensures User.Identity is not null
            }
        };

        // Act
        var result = await _controller.DeletePost(postId, homefeed);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }
    
    //negative test for delete post when post does not exist
    [Fact]
    public async Task DeletePost_PostDoesNotExist()
    {
        // Arrange
        var postId = "testPostId";
        var userId = "testUserId";
        var homefeed = true;

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(repo => repo.GetPostByIdAsync(postId)).ReturnsAsync((Post?)null);

        // Act
        var result = await _controller.DeletePost(postId, homefeed);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
    
    //negative test for delete post when repository fails to delete post
    [Fact]
    public async Task DeletePost_RepositoryFails_ReturnsRedirectToHome()
    {
        // Arrange
        var postId = "testPostId";
        var userId = "testUserId";
        var homefeed = true;

        var post = new Post("testUserId", "Test content")
        {
            PostId = postId,
            UserId = userId
        };

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(repo => repo.GetPostByIdAsync(postId)).ReturnsAsync(post);
        _mockPostRepository.Setup(repo => repo.DeletePostAsync(postId, userId)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeletePost(postId, homefeed);

        // Assert
        Assert.IsType<BadRequestResult>(result);
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error deleting post from database.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

//DELETE IMAGES FROM FILE SYSTEM METHOD
    //positive test for deleting images from file system
    [Fact]
    public void DeleteImageFiles_ValidImages()
    {
        //arrange 
        var wwwRoothPath = "wwwroot";
        var imageUrl = "uploads/test.jpg";
        var filepath = Path.Combine(wwwRoothPath, "uploads", "test.jpg");
        
        _mockWebHostEnvironment.Setup (e => e.WebRootPath).Returns(wwwRoothPath);
        File.Create(filepath).Dispose();  //create a dummy file
        
        //act
        _controller.DeleteImageFile(imageUrl);
        
        //assert 
        Assert.False(File.Exists(filepath));
    }

//RETURN SAVEDPOST VIEW METHOD
    //positive test for when user has saved posts
    [Fact]
    public async Task SavedPosts_UserHasSaved()
    {
        // Arrange
        var userId = "testUserId";
        var savedPosts = new List<Post>
        {
            new Post(userId, "Test content")
            {
                PostId = "testPostId",
                User = new ApplicationUser { UserName = "testUser", ProfilePictureUrl = "testProfilePicUrl" },
                Images = new List<PostImage>(),
                Likes = new List<Like>(),
                Comments = new List<Comment>()
            }
        };

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(repo => repo.GetSavedPostsByUserIdAsync(userId)).ReturnsAsync(savedPosts);

        // Act
        var result = await _controller.SavedPosts();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<PostViewModel>>(viewResult.Model);
        Assert.NotEmpty(model);
        Assert.Equal(savedPosts.Count, model.Count);
        Assert.Equal(savedPosts.First().PostId, model.First().PostId);
    }
    
    //negative test for when user is not authenticated
    [Fact]
    public async Task SavedPosts_UnauthenticatedUser()
    {
        // Arrange
        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string?)null);

        // Act
        var result = await _controller.SavedPosts();

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
    
    //negative test for when user has no saved posts 
    [Fact]
    public async Task SavedPosts_UserHasNoSaved()
    {
        // Arrange
        var userId = "testUserId";
        var savedPosts = Enumerable.Empty<Post>().ToList();

        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(repo => repo.GetSavedPostsByUserIdAsync(userId)).ReturnsAsync(savedPosts);

        // Act
        var result = await _controller.SavedPosts();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<PostViewModel>>(viewResult.Model);
        Assert.Empty(model);
    }
}    