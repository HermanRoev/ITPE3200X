using System.Diagnostics;
using System.Reflection;
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

public class HomeControllerTests
{
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mockPostRepository = new Mock<IPostRepository>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            null, null, null, null, null, null, null, null
        );
        _controller = new HomeController(_mockPostRepository.Object, _mockUserManager.Object);
    }
    
//INDEX TESTS
    //positive test for if index returns view with post view models
    [Fact]
    public async Task Index_ReturnsViewWithPosts()
    {
        //arrange
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
        _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("currentUserId");

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<PostViewModel>>(viewResult.Model);
        Assert.Equal(posts.Count, model.Count);
    }
    
    //negative test for index method if it returns view with no posts
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
    
    //negativ test for index method when GetAllPostsAsync throws an exception
    [Fact]
    public async Task Index_ThrowsException()
    {
        // Arrange
        _mockPostRepository.Setup(repo => repo.GetAllPostsAsync()).ThrowsAsync(new Exception());

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.Model); // Assuming null model indicates an error was handled
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
        var result = (string)methodInfo.Invoke(_controller, new object[] { createdAt });

        // Assert
        Assert.Equal("30 m ago", result);
    }
    
//ERROR METHOD 
    //positive test for that Error returns with a ViewModel containing a valid requestId 
    [Fact]
    public void Error_ReturnsViewWithRequestId()
    {
        // Arrange
        Activity.Current = new Activity("test").Start();

        // Act
        var result = _controller.Error();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
        Assert.Equal(Activity.Current.Id, model.RequestId);
        Activity.Current.Stop();
    }
    
    //negative test for that Error handles null Activity.Current.Id 
    [Fact]
    public void Error_ReturnsViewWithNullRequestId()
    {
        
    }
    
}

