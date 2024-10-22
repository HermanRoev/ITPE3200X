using ITPE3200X.Models;

namespace ITPE3200X.ViewModels;

public class ProfileViewModel
{
    public ApplicationUser User { get; set; } //Her ligger followers, bio, profilbilde, litt andre greier
    public List<PostViewModel>? Posts { get; set; } // Her ligger alt av post griene YIPPI
}