using ITPE3200X.Models;

namespace ITPE3200X.ViewModels;

public class ProfileViewModel
{
    public ApplicationUser? User { get; set; } 
    public List<PostViewModel>? Posts { get; set; } 
    public bool IsCurrentUserProfile { get; set; } 
    public bool IsFollowing { get; set; } 
}