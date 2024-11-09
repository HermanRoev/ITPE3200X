using System.Security.Claims;
using ITPE3200X.Controllers;
using ITPE3200X.DAL.Repositories;
using ITPE3200X.Models;
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
            Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
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
    public async Task CreatePost_ReturnsViewWithPost()
    {
        // Act
        var result = _controller.CreatePost() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ViewResult>(result);
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
        // Arrange
        var postId = "testPostId";
        var userId = "testUserId";

        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        _mockPostRepository.Setup(r => r.AddLikeAsync(postId, userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ToggleLike(postId, false) as OkResult;

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
        Assert.NotNull(result);
    }
}    