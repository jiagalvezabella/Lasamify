using Microsoft.EntityFrameworkCore;
using Lasamify.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=lasamify.db"));

builder.Services.AddAuthentication("LasamifyCookies")
    .AddCookie("LasamifyCookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddSession();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    // Seed sample data if database is empty
    if (!db.Products.Any())
    {
        var sampleSeller = new Lasamify.Models.User
        {
            Username = "sampleseller",
            Email = "seller@lasamify.local",
            PasswordHash = "sample", // In production, this should be properly hashed
            Role = "Seller",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(sampleSeller);
        db.SaveChanges();

        var sampleProducts = new List<Lasamify.Models.Product>
        {
            new()
            {
                Name = "Designer Shirt",
                Description = "High-quality designer shirt in excellent condition. Perfect for daily use or any occasion.",
                Price = 45.00m,
                Category = "Fashion",
                Stock = 1,
                IsAvailable = true,
                ImagePath = "/sample_items/bape.jpg",
                SellerId = sampleSeller.Id,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "Harry Potter Book",
                Description = "Popular fantasy novel by J.K. Rowling. Great for all ages and book collectors.",
                Price = 25.00m,
                Category = "Books",
                Stock = 2,
                IsAvailable = true,
                ImagePath = "/sample_items/book.jpg",
                SellerId = sampleSeller.Id,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "Happy Meal",
                Description = "Fresh and delicious homemade meal prepared daily. Available for pickup or delivery.",
                Price = 12.00m,
                Category = "Food",
                Stock = 5,
                IsAvailable = true,
                ImagePath = "/sample_items/foodmeal.jpg",
                SellerId = sampleSeller.Id,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "iPhone 20 Pro Max",
                Description = "Recent model smartphone in mint condition. All accessories included.",
                Price = 350.00m,
                Category = "Electronics",
                Stock = 1,
                IsAvailable = true,
                ImagePath = "/sample_items/phone.jpg",
                SellerId = sampleSeller.Id,
                CreatedAt = DateTime.UtcNow
            }
        };

        db.Products.AddRange(sampleProducts);
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}");

app.Run();
