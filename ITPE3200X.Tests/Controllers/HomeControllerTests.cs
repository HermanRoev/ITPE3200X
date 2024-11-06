using System.Drawing;
using System.Security.Claims;
using ITPE3200X.Controllers;
using ITPE3200X.DAL.Repositories;
using ITPE3200X.Models;
using ITPE3200X.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
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

        _controller = new HomeController(
            _mockLogger.Object,
            _mockPostRepository.Object,
            _mockUserRepository.Object,
            _mockUserManager.Object,
            _mockWebHostEnvironment.Object
        );
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
        _mockUserManager.Setup(manager => manager.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns("1");

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<PostViewModel>>(viewResult.ViewData.Model);
        Assert.Empty(model);
    }
    
//GETPOSTVIEWMODELBYID TESTS    
    //positive test for if index returns view with post view models
    [Fact]
    public async Task GetPostViewModelById_ReturnsPostViewModel()
    {
        // Arrange
        var post = new Post("1", "Test Content")
        {
            PostId = "1",
            Images = new List<PostImage>(),
            User = new ApplicationUser { UserName = "TestUser", ProfilePictureUrl = "/images/profile.png" },
            Likes = new List<Like>(),
            SavedPosts = new List<SavedPost>(),
            Comments = new List<Comment>()
        };
        _mockPostRepository.Setup(repo => repo.GetPostByIdAsync("1")).ReturnsAsync(post);
        _mockUserManager.Setup(manager => manager.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns("1");

        // Act
        var result = await _controller.GetPostViewModelById("1", true);

        // Assert
        var model = Assert.IsType<PostViewModel>(result);
        Assert.Equal("Test Content", model.Content);
        Assert.Equal("TestUser", model.UserName);
        Assert.False(model.IsLikedByCurrentUser);
        Assert.False(model.IsSavedByCurrentUser);
        Assert.True(model.IsOwnedByCurrentUser);
    }
    
    //negative test for GetPostViewModelById method
    //fungerer ikke enda 
    [Fact]
    public async Task GetPostViewModelById_ReturnsNull()
    {
        
    }
    
//TOGGLELIKE TESTS 
    //positive test for ToggleLike method
    [Fact]
    public async Task ToggleLike_ReturnsTrue()
    {

    }
    
    //negative test for ToggleLike method
    [Fact]
    public async Task ToggleLike_ReturnsFalse()
    {
        //arrange 
        var postId = "post1";
        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string)null);

        // Act
        var result = _controller.ToggleLike(postId, true, true);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }
    
//TOGGLESAVE TESTS
    //positive test for ToggleSave method 
    [Fact]
    public async Task ToggleSave_ReturnsTrue()
    {
        
    }
    
    
    //negative test for ToggleSave method
    [Fact]
    public async Task ToggleSave_ReturnsFalse()
    {
       
    }
    
}

