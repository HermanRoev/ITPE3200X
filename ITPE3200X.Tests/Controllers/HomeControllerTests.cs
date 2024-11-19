using System.Reflection;
using ITPE3200X.Controllers;
using ITPE3200X.DAL.Repositories;
using ITPE3200X.Models;
using ITPE3200X.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ITPE3200X.Tests.Controllers;

public class HomeControllerTests
{
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ILogger<HomeController>> _mockLogger;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mockPostRepository = new Mock<IPostRepository>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
        );
        _mockLogger = new Mock<ILogger<HomeController>>();

        _controller = new HomeController(_mockPostRepository.Object, _mockUserManager.Object, _mockLogger.Object);
    }
    
//INDEX TESTS
    //positive test for if index returns view with post view models
    [Fact]
    public async Task Index_ReturnsViewWithPosts()
    {
        //arrange 
        var posts = new List<Post>()
        {
            new Post("1", "test content")
            {
                PostId = "1",
                Content = "test content",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                User = new ApplicationUser { UserName = "TestUser", Id="1"},
                Likes = new List<Like>(),
                SavedPosts = new List<SavedPost>(),
                Comments = new List<Comment>()
            }
        };

        _mockPostRepository.Setup(repo => repo.GetAllPostsAsync()).ReturnsAsync(posts);
        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns("1");
        
        //act 
        var result = await _controller.Index();
        
        //assert 
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<PostViewModel>>(viewResult.ViewData.Model);
        Assert.Equal(posts.Count, model.Count);
    }   
    
    //negative test for index method if it returns view with no posts
    [Fact]
    public async Task Index_ReturnsViewWithNoPosts()
    {
        //arrange 
        _mockPostRepository.Setup(repo => repo.GetAllPostsAsync()).ReturnsAsync((List<Post>)null);
        
        //act 
        var result = await _controller.Index();
        
        //assert 
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewData.Model);

    }
    
//CALCULATEDTIMESINCEPOSTED METHOD 
    //positive test for CalculateTimeSincePosted method when time is less than a hour ago 
    [Fact]
    public void CalculateTimeSincePosted_LessThanHourAgo()
    //funker men vet ikke om vi skal ta med testing for denne metoden siden den er privat
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddMinutes(-30);

        // Use reflection to get the private method
        var methodInfo = typeof(HomeController).GetMethod("CalculateTimeSincePosted", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        // Act
        var result = (string)methodInfo.Invoke(_controller, new object[] { createdAt })!;

        // Assert
        Assert.Equal("30 m ago", result);
    }
    
    //positive test for CalculateTimeSincePosted method when time is less than 24 hours ago
    [Fact]
    public void CalculateTimeSincePosted_LessThanDayAgo()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddHours(-10);

        // Use reflection to get the private method
        var methodInfo = typeof(HomeController).GetMethod("CalculateTimeSincePosted", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        // Act
        var result = (string)methodInfo.Invoke(_controller, new object[] { createdAt })!;

        // Assert
        Assert.Equal("10 h ago", result);
        
    }
    
    //positive test for CalculateTimeSincePosted method when time is more than a day ago
    [Fact]
    public void CalculateTimeSincePosted_MoreThanDayAgo()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-2);

        // Use reflection to get the private method
        var methodInfo = typeof(HomeController).GetMethod("CalculateTimeSincePosted", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        // Act
        var result = (string)methodInfo.Invoke(_controller, new object[] { createdAt })!;

        // Assert
        Assert.Equal("2 d ago", result);
        
    }
}

