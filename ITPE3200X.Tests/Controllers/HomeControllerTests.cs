using System.Drawing;
using ITPE3200X.Controllers;
using ITPE3200X.DAL.Repositories;
using ITPE3200X.Models;
using Microsoft.AspNetCore.Identity;
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
        _mockUserManager = MockUserManager<ApplicationUser>();
        
        _controller = new HomeController(_mockPostRepository.Object, _mockUserManager.Object);
    }
    
    [Fact]
    public async Task TestIndex_ReturnsViewWithPostViewModels()
    {
        
    }

