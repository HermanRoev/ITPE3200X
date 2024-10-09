using System.Diagnostics;
using ITPE3200X.DAL.Repositories;
using Microsoft.AspNetCore.Mvc;
using ITPE3200X.Models;
using Microsoft.AspNetCore.Identity;

namespace ITPE3200X.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IPostRepository _postRepository;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public HomeController(ILogger<HomeController> logger, IPostRepository postRepository, SignInManager<ApplicationUser> signInManager)
    {
        _logger = logger;
        _postRepository = postRepository;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> Index()
    {
        var posts = await _postRepository.GetAllPostsAsync();
        return View(posts);
    }

    // Eksempel metode for å legge til en kommentar, sånn man skal bruke controlleren
    [HttpPost]
    public async void AddComment(string postId, string content)
    {
        var userId = _signInManager.UserManager.GetUserId(User);
        var comment = new Comment(postId, userId, content);
        await _postRepository.AddCommentAsync(comment);
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
        return View();
    }

}