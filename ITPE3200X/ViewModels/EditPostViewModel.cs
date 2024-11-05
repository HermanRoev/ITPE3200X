using ITPE3200X.Models;

namespace ITPE3200X.ViewModels;

public class EditPostViewModel
{
    public ICollection<PostImage>? Images { get; set; }
    public string? Content { get; set; }
    public string? PostId { get; set; }

}