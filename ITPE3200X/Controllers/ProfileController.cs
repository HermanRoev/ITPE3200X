using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITPE3200X.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // GET: Profile
    public async Task<IActionResult> Index(String userId)
    {
        var user = await _userManager.Users
            .Include(u => u.Followers)
            .Include(u => u.Following)
            .Include(u => u.Posts)
            .FirstOrDefaultAsync(); // Fjerner autentisering for å hente en bruker direkte

        return View("Profile", user);
    }

    // GET: EditProfile
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(); // Henter første bruker
        return View("Edit", user);
    }

    // POST: EditProfile
    [HttpPost]
    public async Task<IActionResult> Edit(ApplicationUser updatedUser)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", updatedUser);
        }

        var user = await _userManager.Users.FirstOrDefaultAsync();
        if (user != null)
        {
            user.Bio = updatedUser.Bio;
            user.ProfilePictureUrl = updatedUser.ProfilePictureUrl;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Kunne ikke oppdatere profil");
                return View("Edit", updatedUser);
            }
        }

        return RedirectToAction("Index");
    }
}







