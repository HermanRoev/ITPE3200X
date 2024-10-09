using ITPE3200X.Models;
using Microsoft.AspNetCore.Identity;

namespace ITPE3200X.DAL
{
    public static class DBInit
    {
        public static void Seed(IApplicationBuilder app)
        {
            // Create a new scope to retrieve scoped services
            using var serviceScope = app.ApplicationServices.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Optionally delete and recreate the database for testing
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Retrieve UserManager to create users
            var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Seed data if the database is empty
            if (!context.Users.Any())
            {
                // Create users
                var users = new List<ApplicationUser>
                {
                    new ApplicationUser { UserName = "user1", Email = "user1@example.com" },
                    new ApplicationUser { UserName = "user2", Email = "user2@example.com" },
                    new ApplicationUser { UserName = "user3", Email = "user3@example.com" }
                };

                foreach (var user in users)
                {
                    var result = userManager.CreateAsync(user, "Password123!").Result;
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to create user {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }

                // Retrieve created users
                var user1 = userManager.FindByNameAsync("user1").Result;
                var user2 = userManager.FindByNameAsync("user2").Result;
                var user3 = userManager.FindByNameAsync("user3").Result;

                // Create posts
                var posts = new List<Post>
                {
                    new Post(userId: user1.Id, content: "This is the first test post.", title: "First post") { CreatedAt = DateTime.UtcNow.AddMinutes(-30) },
                    new Post(userId: user2.Id, content: "This is the second test post.", title: "Second post") { CreatedAt = DateTime.UtcNow.AddMinutes(-20) },
                    new Post(userId: user3.Id, content: "This is the third test post.", title: "Third post") { CreatedAt = DateTime.UtcNow.AddMinutes(-10) }
                };
                context.Posts.AddRange(posts);
                context.SaveChanges();

                // Retrieve created posts
                var post1 = posts[0];
                var post2 = posts[1];
                var post3 = posts[2];

                // Create post images
                var postImages = new List<PostImage>
                {
                    new PostImage(postId: post1.PostId, imageUrl: "/images/image1.jpg") { CreatedAt = DateTime.UtcNow.AddMinutes(-29) },
                    new PostImage(postId: post2.PostId, imageUrl: "/images/image2.jpg") { CreatedAt = DateTime.UtcNow.AddMinutes(-19) },
                    new PostImage(postId: post3.PostId, imageUrl: "/images/image3.jpg") { CreatedAt = DateTime.UtcNow.AddMinutes(-9) }
                };
                context.PostImages.AddRange(postImages);
                context.SaveChanges();
            }
        }
    }
}