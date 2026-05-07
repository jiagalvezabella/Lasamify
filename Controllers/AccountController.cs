using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lasamify.Data;
using Lasamify.Models;

namespace Lasamify.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Account/Register
        public IActionResult Register() => View();

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            if (await _context.Users.AnyAsync(u => u.Email == vm.Email))
            {
                ModelState.AddModelError("Email", "Email is already in use.");
                return View(vm);
            }

            var user = new User
            {
                Username = vm.Username,
                Email = vm.Email,
                PasswordHash = HashPassword(vm.Password),
                Role = vm.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Account created! Please log in.";
            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        public IActionResult Login() => View();

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == vm.Email);

            if (user == null || user.PasswordHash != HashPassword(vm.Password))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(vm);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, "LasamifyCookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("LasamifyCookies", principal, new AuthenticationProperties
            {
                IsPersistent = vm.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });

            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("LasamifyCookies");
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Profile
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users
                .Include(u => u.Products)
                    .ThenInclude(p => p.Transactions)
                        .ThenInclude(t => t.Buyer)
                .Include(u => u.BuyerTransactions)
                    .ThenInclude(t => t.Product)
                        .ThenInclude(p => p.Seller)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return View(user);
        }

        // POST: /Account/UploadProfilePicture
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            try
            {
                if (profilePicture == null || profilePicture.Length == 0)
                {
                    TempData["Error"] = "Please select a valid image.";
                    return RedirectToAction("Profile");
                }

                // Validate file size (max 5MB)
                const long maxFileSize = 5 * 1024 * 1024; // 5MB
                if (profilePicture.Length > maxFileSize)
                {
                    TempData["Error"] = "File size must be less than 5MB.";
                    return RedirectToAction("Profile");
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(profilePicture.FileName).ToLower();
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Only image files (JPG, PNG, GIF, WEBP) are allowed.";
                    return RedirectToAction("Profile");
                }

                // Get user ID from claims
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    TempData["Error"] = "User not authenticated properly.";
                    return RedirectToAction("Login");
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Login");
                }

                // Check if WebRootPath is set
                if (string.IsNullOrEmpty(_env.WebRootPath))
                {
                    TempData["Error"] = "Server configuration error.";
                    return RedirectToAction("Profile");
                }

                // Create uploads directory structure
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                try
                {
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                }
                catch (Exception dirEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Directory creation error: {dirEx.Message}");
                    TempData["Error"] = "Failed to create upload directory.";
                    return RedirectToAction("Profile");
                }

                // Generate unique filename
                var fileName = $"user_{userId}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                try
                {
                    // Delete old profile picture if exists
                    if (!string.IsNullOrEmpty(user.ProfilePicturePath))
                    {
                        var oldFilePath = Path.Combine(_env.WebRootPath, user.ProfilePicturePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            catch
                            {
                                // Log but don't fail if we can't delete old file
                            }
                        }
                    }

                    // Save file with proper disposal
                    await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await profilePicture.CopyToAsync(fileStream);
                        await fileStream.FlushAsync();
                    }
                }
                catch (Exception fileEx)
                {
                    System.Diagnostics.Debug.WriteLine($"File save error: {fileEx.Message}");
                    TempData["Error"] = $"Failed to save file: {fileEx.Message}";
                    return RedirectToAction("Profile");
                }

                try
                {
                    // Update user profile picture path
                    user.ProfilePicturePath = $"/uploads/profiles/{fileName}";
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Database save error: {dbEx.Message}");
                    TempData["Error"] = "Profile picture uploaded but database update failed.";
                    return RedirectToAction("Profile");
                }

                TempData["Success"] = "Profile picture updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                // Log the error (in production, use proper logging)
                System.Diagnostics.Debug.WriteLine($"Profile picture upload error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = $"An error occurred while uploading the picture: {ex.Message}";
                return RedirectToAction("Profile");
            }
        }

        // POST: /Account/UploadTransactionReceipt
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTransactionReceipt(int transactionId, IFormFile receipt)
        {
            try
            {
                if (receipt == null || receipt.Length == 0)
                {
                    TempData["Error"] = "Please select a valid receipt image.";
                    return RedirectToAction("Profile");
                }

                // Validate file size (max 5MB)
                const long maxFileSize = 5 * 1024 * 1024; // 5MB
                if (receipt.Length > maxFileSize)
                {
                    TempData["Error"] = "File size must be less than 5MB.";
                    return RedirectToAction("Profile");
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };
                var ext = Path.GetExtension(receipt.FileName).ToLower();
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Only image files (JPG, PNG, GIF, WEBP) or PDF are allowed.";
                    return RedirectToAction("Profile");
                }

                // Get user ID from claims
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    TempData["Error"] = "User not authenticated properly.";
                    return RedirectToAction("Login");
                }

                // Check if WebRootPath is set
                if (string.IsNullOrEmpty(_env.WebRootPath))
                {
                    TempData["Error"] = "Server configuration error.";
                    return RedirectToAction("Profile");
                }

                // Create receipts directory
                var receiptsFolder = Path.Combine(_env.WebRootPath, "uploads", "receipts");
                if (!Directory.Exists(receiptsFolder))
                {
                    Directory.CreateDirectory(receiptsFolder);
                }

                // Generate unique filename
                var fileName = $"receipt_{transactionId}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(receiptsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await receipt.CopyToAsync(stream);
                }

                // In a real app, you'd update a Receipt entity in the database
                // For now, we'll just show success
                TempData["Success"] = "Receipt uploaded successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Receipt upload error: {ex.Message}");
                TempData["Error"] = $"An error occurred while uploading the receipt: {ex.Message}";
                return RedirectToAction("Profile");
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "LasamifySalt_2024"));
            return Convert.ToBase64String(bytes);
        }
    }
}
