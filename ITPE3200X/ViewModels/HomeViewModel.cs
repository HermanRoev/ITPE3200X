using ITPE3200X.Models;

namespace ITPE3200X.ViewModels
{
    public class HomeViewModel
    {
        public ApplicationUser User { get; set; } // Current logged-in user
        public List<PostViewModel>? Posts { get; set; } // List of posts to display on the home page
        public bool IsHomePage { get; set; } // Optional: could help in conditional rendering if needed
    }
}