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
    private readonly Mock<IPostRepository> _mockPostRepository; 
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ILogger<HomeController>> _mockLogger;
    private readonly HomeController _controller;

    public ProfileControllerTests()
    {
        _mockPostRepository = new Mock<IPostRepository>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
        );
        _mockLogger = new Mock<ILogger<HomeController>>();

        _controller = new HomeController(
            _mockPostRepository.Object, 
            _mockUserManager.Object, 
            _mockLogger.Object);
    }
        
        
    [Fact] 
    public async Task Profile_usernameExists_RetunView()
    {

    }
}
    

    
    
    

//CALCULATE TIME METHOD 

//EDIT PROFILE VIEW METHOD 

//DELETE IMAGES FROM FILESYSTEM 

//FOLLOW METHOD 

//UNFOLLOW METHOD


