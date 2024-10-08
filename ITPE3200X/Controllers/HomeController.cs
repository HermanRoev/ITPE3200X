using System.Diagnostics;
using ITPE3200X.DAL.Repositories;
using Microsoft.AspNetCore.Mvc;
using ITPE3200X.Models;

namespace ITPE3200X.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IPostRepository _postRepository;

    public HomeController(ILogger<HomeController> logger, IPostRepository postRepository)
    {
        _logger = logger;
        _postRepository = postRepository;
    }

    public async Task<IActionResult> Index()
    {
        var posts = await _postRepository.GetAllPostsAsync();
        return View(posts);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
    public IActionResult Profile()
    {
        // Du må hente brukerdata her fra databasen, men for enkelhets skyld kan vi først hardkode noe
        var model = new ProfileViewModel
        {
            UserName = "JohnDoe",
            Email = "johndoe@example.com",
            ProfilePictureUrl = "/images/default_profile.png", // Sett inn riktig bilde-URL her
            Bio = "I love coding and sharing my work!",
            FollowersCount = 120,
            FollowingCount = 75
        };

        return View(model);
    }

}