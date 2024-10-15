using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITPE3200X.Models;
using System.Linq;
using System.Threading.Tasks;
using ITPE3200X.DAL.Repositories;
using Microsoft.AspNetCore.Identity;

public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _userRepository;

    public ProfileController(UserManager<ApplicationUser> userManager, IUserRepository userRepository)
    {
        _userManager = userManager;
        _userRepository = userRepository;
    }

    // GET: Profile
    public async Task<IActionResult> Profile()
    {
        var userId = _userManager.GetUserId(User);
        var user = _userRepository.GetUserdataById(userId).Result;
        return View(user);
    }

    // GET: EditProfile
    public async Task<IActionResult> Edit()
    {
        var userId = _userManager.GetUserId(User);
        var user = _userRepository.GetUserdataById(userId).Result;
        return View(user);
    }
    
    [HttpPost]
    public async Task<IActionResult> Edit(ApplicationUser model)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Index");
        }

        user.ProfilePictureUrl = model.ProfilePictureUrl;
        user.Bio = model.Bio;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Kunne ikke oppdatere profil.");
            return View(model);
        }

        return RedirectToAction("Profile");
    }

}







