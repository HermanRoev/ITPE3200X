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

public class ProfileControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly Mock<ILogger<ProfileController>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private readonly ProfileController _controller;

    public ProfileControllerTests()
    {
        // Mock UserManager dependencies
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        _mockUserRepository = new Mock<IUserRepository>();
        _mockPostRepository = new Mock<IPostRepository>();
        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<ProfileController>>();

        // Initialize the controller with mocked dependencies
        _controller = new ProfileController(
            _mockUserManager.Object,
            _mockUserRepository.Object,
            _mockPostRepository.Object,
            _mockWebHostEnvironment.Object,
            _mockLogger.Object
        );
    }

//PROFILE METHOD 
    //positive test: username provided and authenticated
    [Fact]
    public async Task Profile_UsernameExists_authenticated()
    {
        //arrange
        string username = "testuser";
        var user = new ApplicationUser { UserName = username, Id = "testuserid" };
        var posts = new List<Post>(); // Assume posts are populated
        
        // Mock the authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName)
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        
        // Setup mocks
        _mockUserManager.Setup(x => x.GetUserName(principal)).Returns(user.UserName);
        _mockUserManager.Setup(x => x.FindByNameAsync(user.UserName)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetUserId(principal)).Returns(user.Id);
        _mockPostRepository.Setup(x => x.GetPostsByUserAsync(user.Id)).ReturnsAsync(posts);
        _mockUserRepository.Setup(x => x.IsFollowingAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        
        // Act
        var result = await _controller.Profile(username);
        
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<ProfileViewModel>(viewResult.Model);
        Assert.Equal(user, model.User);
    }

    //positive test: usermame is null and authenticated
    [Fact]
    public async Task Profile_UsernameIsNull_authenticated_ReturnView()
    {
        // Arrange
        string username = null;
        var currentUser = new ApplicationUser { UserName = "currentuser", Id = "currentuserid" };
        var posts = new List<Post>(); // Assume posts are populated

        // Mock the authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, currentUser.Id),
            new Claim(ClaimTypes.Name, currentUser.UserName)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Setup mocks
        _mockUserManager.Setup(x => x.GetUserName(principal)).Returns(currentUser.UserName);
        _mockUserManager.Setup(x => x.FindByNameAsync(currentUser.UserName)).ReturnsAsync(currentUser);
        _mockUserManager.Setup(x => x.GetUserId(principal)).Returns(currentUser.Id);
        _mockPostRepository.Setup(x => x.GetPostsByUserAsync(currentUser.Id)).ReturnsAsync(posts);
        _mockUserRepository.Setup(x => x.IsFollowingAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        // Act
        var result = await _controller.Profile(username);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<ProfileViewModel>(viewResult.Model);
        Assert.Equal(currentUser, model.User);
    }
    
    //negative test: usermame is null and not authenticated
    [Fact]
    public async Task Profile_UsernameIsNull_notAuthenticated()
    {
        //arrange 
        string username = null;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        _mockUserManager.Setup(um => um.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns((string)null);

        // Act
        var result = await _controller.Profile(username);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found", notFoundResult.Value);
    }
    
    //negativ test: username provided but user does not exist 
    [Fact]
    public async Task Profile_UsernameProvided_ButNotFound()
    {
        // Arrange
        string username = "nonexistentuser";
        _mockUserManager.Setup(x => x.FindByNameAsync(username)).ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _controller.Profile(username);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found", notFoundResult.Value);
    }

//EDIT PROFILE VIEW METHOD (GET)
    //positive test: user is authenticated
    [Fact]
    public async Task EditProfileGet_authenticated_ReturnView()
    {
        // Arrange
        var currentUser = new ApplicationUser
        {
            UserName = "currentuser",
            Id = "currentuserid",
            Bio = "Bio",
            ProfilePictureUrl = "/path/to/picture.jpg"
        };

        // Mock the authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, currentUser.Id),
            new Claim(ClaimTypes.Name, currentUser.UserName)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Setup mocks
        _mockUserManager.Setup(x => x.GetUserAsync(principal)).ReturnsAsync(currentUser);

        // Act
        var result = await _controller.Edit();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<EditProfileViewModel>(viewResult.Model);
        Assert.Equal(currentUser.Bio, model.Bio);
        Assert.Equal(currentUser.ProfilePictureUrl, model.ProfilePictureUrl);
    }
    
    //negative test: user is not authenticated
    [Fact]
    public async Task EditProfileGet_notAuthenticated()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _controller.Edit();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }
    
//EDIT PROFILE METHOD (POST)  
    //positive test: valid model, image file is valid 
    [Fact]
    public async Task EditProfilePost_ValidModel_ValidImage()
    {
        // Arrange 
        var user = new ApplicationUser
        {
            UserName = "testuser",
            ProfilePictureUrl = "/path/to/picture.jpg",
            Bio = "Bio"
        };

        var model = new EditProfileViewModel
        {
            Bio = "test new bio",
            ProfilePictureUrl = "/path/to/picture.jpg",
            ImageFile = new FormFile(Mock.Of<Stream>(), 0, 100, "profilePicture", "profilePicture.jpg")
        };
        
        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        
        _mockWebHostEnvironment.Setup(whe => whe.WebRootPath).Returns("wwwroot");
        
        // Act
        var result = await _controller.Edit(model);
        
        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Profile", redirectResult.ActionName);
        Assert.Equal("testuser", redirectResult.RouteValues["username"]);
    }
    
    //negative test: invalid model
    [Fact]
    public async Task EditProfilePost_InvalidModel_InvalidImage()
    {
        // Arrange
        _controller.ModelState.AddModelError("Bio", "Required");
        var model = new EditProfileViewModel();

        // Act
        var result = await _controller.Edit(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<EditProfileViewModel>(viewResult.Model);
        Assert.Same(model, returnedModel);
        Assert.False(_controller.ModelState.IsValid);
    }
    
    //negative test: invalid image file
    [Fact]
    public async Task EditProfilePost_ValidModel_InvalidImage()
    {
        // Arrange
        var currentUser = new ApplicationUser
        {
            UserName = "currentuser",
            Id = "currentuserid",
            Bio = "Bio",
            ProfilePictureUrl = "/path/to/picture.jpg"
        };

        var model = new EditProfileViewModel
        {
            Bio = "New bio",
            ImageFile = new FormFile(null, 0, 0, "profilePicture", "profilePicture.txt")
        };

        // Mock the authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, currentUser.Id),
            new Claim(ClaimTypes.Name, currentUser.UserName)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockUserManager.Setup(x => x.GetUserAsync(principal)).ReturnsAsync(currentUser);

        // Act
        var result = await _controller.Edit(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<EditProfileViewModel>(viewResult.Model);
        Assert.Same(model, returnedModel);
        Assert.False(_controller.ModelState.IsValid);
    }
    
    //negative test: user is not authenticated
    [Fact]
    public async Task EditProfilePost_notAuthenticated()
    {
        // Arrange
        var model = new EditProfileViewModel
        {
            Bio = "New Bio",
            ProfilePictureUrl = "valid-url",
            ImageFile = new Mock<IFormFile>().Object
        };

        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _controller.Edit(model);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User not found", unauthorizedResult.Value);
    }

//FOLLOW METHOD 
    //positive test: user to follow exists, follow succeeds 
    [Fact]
    public async Task Follow_UserExists_FollowSucceeds()
    {
        // Arrange
        var userToFollow = new ApplicationUser { UserName = "usertofollow", Id = "usertofollowid" };
        var currentUserId = "currentuserid";

        // Mock the authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId),
            new Claim(ClaimTypes.Name, "currentuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Setup mocks
        _mockUserManager.Setup(x => x.FindByNameAsync(userToFollow.UserName)).ReturnsAsync(userToFollow);
        _mockUserManager.Setup(x => x.GetUserId(principal)).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.AddFollowerAsync(currentUserId, userToFollow.Id)).ReturnsAsync(true);

        // Act
        var result = await _controller.Follow(userToFollow.UserName);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Profile", redirectResult.ActionName);
        Assert.Equal(userToFollow.UserName, redirectResult.RouteValues["username"]);
    }
    
    //negative test: user to follow does not exist
    [Fact]
    public async Task Follow_UserDoesNotExist()
    {
        //Arrange
        string username = "nonexistentuser";
        _mockUserManager.Setup(x => x.FindByNameAsync(username)).ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _controller.Follow(username);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found", notFoundResult.Value);
    }
    
    //negative test: follow action fails 
    [Fact]
    public async Task Follow_FollowActionFails()
    {
        // Arrange
        var userToFollow = new ApplicationUser { UserName = "usertofollow", Id = "usertofollowid" };
        var currentUserId = "currentuserid";

        // Mock the authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId),
            new Claim(ClaimTypes.Name, "currentuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Setup mocks
        _mockUserManager.Setup(x => x.FindByNameAsync(userToFollow.UserName)).ReturnsAsync(userToFollow);
        _mockUserManager.Setup(x => x.GetUserId(principal)).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.AddFollowerAsync(currentUserId, userToFollow.Id)).ReturnsAsync(false);

        // Act
        var result = await _controller.Follow(userToFollow.UserName);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Profile", redirectResult.ActionName);
        Assert.Equal(userToFollow.UserName, redirectResult.RouteValues["username"]);
    }
    
//UNFOLLOW METHOD
    //positive test: user to unfollow exists, unfollow succeeds
    [Fact]
    public async Task Unfollow_UserExists_Succeeds()
    {
        // Arrange
        var userToUnfollow = new ApplicationUser { UserName = "usertounfollow", Id = "usertounfollowid" };
        var currentUserId = "currentuserid";

        // Mock the authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId),
            new Claim(ClaimTypes.Name, "currentuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Setup mocks
        _mockUserManager.Setup(x => x.FindByNameAsync(userToUnfollow.UserName)).ReturnsAsync(userToUnfollow);
        _mockUserManager.Setup(x => x.GetUserId(principal)).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.RemoveFollowerAsync(currentUserId, userToUnfollow.Id)).ReturnsAsync(true);

        // Act
        var result = await _controller.Unfollow(userToUnfollow.UserName);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Profile", redirectResult.ActionName);
        Assert.Equal(userToUnfollow.UserName, redirectResult.RouteValues["username"]);
    }
    
    //negative test: user to unfollow does not exist
    [Fact]
    public async Task Unfollow_UserDoesNotExist()
    {
        // Arrange
        string username = "nonexistentuser";
        _mockUserManager.Setup(x => x.FindByNameAsync(username)).ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _controller.Unfollow(username);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found", notFoundResult.Value);
    }
    
    //negative test: unfollow action fails
    [Fact]
    public async Task Unfollow_UnfollowActionFails()
    {
        // Arrange
        var userToUnfollow = new ApplicationUser { UserName = "usertounfollow", Id = "usertounfollowid" };
        var currentUserId = "currentuserid";

        // Mock the authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId),
            new Claim(ClaimTypes.Name, "currentuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Setup mocks
        _mockUserManager.Setup(x => x.FindByNameAsync(userToUnfollow.UserName)).ReturnsAsync(userToUnfollow);
        _mockUserManager.Setup(x => x.GetUserId(principal)).Returns(currentUserId);
        _mockUserRepository.Setup(x => x.RemoveFollowerAsync(currentUserId, userToUnfollow.Id)).ReturnsAsync(false);

        // Act
        var result = await _controller.Unfollow(userToUnfollow.UserName);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Profile", redirectResult.ActionName);
        Assert.Equal(userToUnfollow.UserName, redirectResult.RouteValues["username"]);
    }
}

