using ITPE3200X.Controllers;
using ITPE3200X.DAL.Repositories;
using ITPE3200X.Models;
using ITPE3200X.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ITPE3200X.Tests.Controllers;

public class HomeControllerTests
{
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ILogger<HomeController>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mockLogger = new Mock<ILogger<HomeController>>();
        _mockPostRepository = new Mock<IPostRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            null, null, null, null, null, null, null, null
        );
        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
    }
    
//INDEX TESTS
    //positive test for if index returns view with post view models
    [Fact]
    public async Task Index_ReturnsViewWithPosts()
    {
        // Arrange
        var posts = new List<Post>
        {
            new Post("1", "Test Content")
            {
                PostId = "1",
                Images = new List<PostImage>(),
                User = new ApplicationUser { UserName = "TestUser", ProfilePictureUrl = "/images/profile.png" },
                Likes = new List<Like>(),
                SavedPosts = new List<SavedPost>(),
                Comments = new List<Comment>()
            }
        };
        _mockPostRepository.Setup(repo => repo.GetAllPostsAsync()).ReturnsAsync(posts);
        _mockUserManager.Setup(manager => manager.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns("1");

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<PostViewModel>>(viewResult.ViewData.Model);
        Assert.Single(model);
        Assert.Equal("Test Content", model.First().Content);
        Assert.Equal("TestUser", model.First().UserName);
        Assert.False(model.First().IsLikedByCurrentUser);
        Assert.False(model.First().IsSavedByCurrentUser);
        Assert.True(model.First().IsOwnedByCurrentUser);
    }
    
    //negative test for index method
    [Fact]
    public async Task Index_ReturnsViewWithNoPosts()
    {
        // Arrange
        _mockPostRepository.Setup(repo => repo.GetAllPostsAsync()).ReturnsAsync(new List<Post>());
        _mockUserManager.Setup(manager => manager.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .Returns("1");

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<PostViewModel>>(viewResult.ViewData.Model);
        Assert.Empty(model);
    }
}

