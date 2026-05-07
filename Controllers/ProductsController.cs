using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using Lasamify.Data;
using Lasamify.Models;

namespace Lasamify.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Seller)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            bool hasBought = false;
            bool hasReviewed = false;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                hasBought = await _context.Transactions.AnyAsync(t => t.BuyerId == userId && t.ProductId == id);
                hasReviewed = await _context.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == id);
            }

            ViewBag.HasBought = hasBought;
            ViewBag.HasReviewed = hasReviewed;

            return View(product);
        }

        [Authorize]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var product = new Product
            {
                Name = vm.Name,
                Description = vm.Description,
                Price = vm.Price,
                Category = vm.Category,
                Stock = vm.Stock,
                SellerId = sellerId
            };

            if (vm.Image != null && vm.Image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(vm.Image.FileName).ToLower();
                if (allowedExtensions.Contains(ext))
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "uploads", "products");
                    Directory.CreateDirectory(uploadsFolder);
                    var fileName = $"product_{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await vm.Image.CopyToAsync(stream);

                    product.ImagePath = $"/images/uploads/products/{fileName}";
                }
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product listed successfully!";
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var product = await _context.Products.FindAsync(id);

            if (product == null || product.SellerId != userId) return Forbid();
            return View(product);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product updated)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var product = await _context.Products.FindAsync(id);

            if (product == null || product.SellerId != userId) return Forbid();

            product.Name = updated.Name;
            product.Description = updated.Description;
            product.Price = updated.Price;
            product.Category = updated.Category;
            product.Stock = updated.Stock;
            product.IsAvailable = updated.IsAvailable;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Product updated!";
            return RedirectToAction("Profile", "Account");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var product = await _context.Products.FindAsync(id);

            if (product == null || product.SellerId != userId) return Forbid();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product removed.";
            return RedirectToAction("Profile", "Account");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(int id, int quantity = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var product = await _context.Products.FindAsync(id);

            if (product == null || !product.IsAvailable || product.Stock < quantity)
            {
                TempData["Error"] = "Product is unavailable or insufficient stock.";
                return RedirectToAction("Details", new { id });
            }

            if (product.SellerId == userId)
            {
                TempData["Error"] = "You cannot buy your own product.";
                return RedirectToAction("Details", new { id });
            }

            var transaction = new Transaction
            {
                BuyerId = userId,
                ProductId = id,
                Quantity = quantity,
                TotalAmount = product.Price * quantity,
                Status = "Pending"
            };

            product.Stock -= quantity;
            if (product.Stock == 0) product.IsAvailable = false;

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order placed! Total: ₱{transaction.TotalAmount:N2}";
            return RedirectToAction("Profile", "Account");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var hasBought = await _context.Transactions
                .AnyAsync(t => t.BuyerId == userId && t.ProductId == productId);

            if (!hasBought)
            {
                TempData["Error"] = "You can only review products you have purchased.";
                return RedirectToAction("Details", new { id = productId });
            }

            var existingReview = await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.ProductId == productId);

            if (existingReview)
            {
                TempData["Error"] = "You have already reviewed this product.";
                return RedirectToAction("Details", new { id = productId });
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thank you for your review!";
            return RedirectToAction("Details", new { id = productId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptOrder(int transactionId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var transaction = await _context.Transactions
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Profile", "Account");
            }

            // Verify seller owns the product
            if (transaction.Product?.SellerId != userId)
            {
                TempData["Error"] = "You don't have permission to accept this order.";
                return RedirectToAction("Profile", "Account");
            }

            transaction.SellerStatus = "Accepted";
            transaction.Status = "Accepted";
            transaction.SellerResponseDate = DateTime.UtcNow;

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order accepted successfully!";
            return RedirectToAction("Profile", "Account");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOrder(int transactionId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var transaction = await _context.Transactions
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Profile", "Account");
            }

            // Verify seller owns the product
            if (transaction.Product?.SellerId != userId)
            {
                TempData["Error"] = "You don't have permission to reject this order.";
                return RedirectToAction("Profile", "Account");
            }

            transaction.SellerStatus = "Rejected";
            transaction.Status = "Rejected";
            transaction.SellerResponseDate = DateTime.UtcNow;

            // Restore stock
            if (transaction.Product != null)
            {
                transaction.Product.Stock += transaction.Quantity;
                if (!transaction.Product.IsAvailable)
                {
                    transaction.Product.IsAvailable = true;
                }
                _context.Products.Update(transaction.Product);
            }

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order rejected and stock restored.";
            return RedirectToAction("Profile", "Account");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GenerateReceipt(int transactionId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var transaction = await _context.Transactions
                .Include(t => t.Buyer)
                .Include(t => t.Product)
                    .ThenInclude(p => p!.Seller)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                TempData["Error"] = "Transaction not found.";
                return RedirectToAction("Profile", "Account");
            }

            // Check if user is buyer or seller
            var isBuyer = transaction.BuyerId == userId;
            var isSeller = transaction.Product?.SellerId == userId;

            if (!isBuyer && !isSeller)
            {
                TempData["Error"] = "You don't have permission to view this receipt.";
                return RedirectToAction("Profile", "Account");
            }

            // Create a view model for the receipt
            var receiptVM = new
            {
                TransactionId = transaction.Id,
                BuyerName = transaction.Buyer?.Username ?? "N/A",
                BuyerEmail = transaction.Buyer?.Email ?? "N/A",
                SellerName = transaction.Product?.Seller?.Username ?? "N/A",
                SellerEmail = transaction.Product?.Seller?.Email ?? "N/A",
                ProductName = transaction.Product?.Name ?? "N/A",
                ProductPrice = transaction.Product?.Price ?? 0,
                Quantity = transaction.Quantity,
                TotalAmount = transaction.TotalAmount,
                Status = transaction.Status,
                SellerStatus = transaction.SellerStatus,
                TransactionDate = transaction.TransactionDate,
                SellerResponseDate = transaction.SellerResponseDate
            };

            return View(receiptVM);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DownloadReceipt(int transactionId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var transaction = await _context.Transactions
                .Include(t => t.Buyer)
                .Include(t => t.Product)
                    .ThenInclude(p => p!.Seller)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                TempData["Error"] = "Transaction not found.";
                return RedirectToAction("Profile", "Account");
            }

            // Check if user is buyer or seller
            var isBuyer = transaction.BuyerId == userId;
            var isSeller = transaction.Product?.SellerId == userId;

            if (!isBuyer && !isSeller)
            {
                TempData["Error"] = "You don't have permission to download this receipt.";
                return RedirectToAction("Profile", "Account");
            }

            // Generate receipt content as CSV or text format
            var receiptContent = GenerateReceiptContent(transaction);
            var fileName = $"Receipt_{transaction.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";

            return File(Encoding.UTF8.GetBytes(receiptContent), "text/plain", fileName);
        }

        private string GenerateReceiptContent(Transaction transaction)
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine("                    RECEIPT");
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Receipt #: {transaction.Id}");
            sb.AppendLine($"Date: {transaction.TransactionDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────────");
            sb.AppendLine("BUYER INFORMATION");
            sb.AppendLine("─────────────────────────────────────────");
            sb.AppendLine($"Name: {transaction.Buyer?.Username ?? "N/A"}");
            sb.AppendLine($"Email: {transaction.Buyer?.Email ?? "N/A"}");
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────────");
            sb.AppendLine("SELLER INFORMATION");
            sb.AppendLine("─────────────────────────────────────────");
            sb.AppendLine($"Name: {transaction.Product?.Seller?.Username ?? "N/A"}");
            sb.AppendLine($"Email: {transaction.Product?.Seller?.Email ?? "N/A"}");
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────────");
            sb.AppendLine("TRANSACTION DETAILS");
            sb.AppendLine("─────────────────────────────────────────");
            sb.AppendLine($"Product: {transaction.Product?.Name ?? "N/A"}");
            sb.AppendLine($"Unit Price: ₱{transaction.Product?.Price:N2}");
            sb.AppendLine($"Quantity: {transaction.Quantity}");
            sb.AppendLine($"Total Amount: ₱{transaction.TotalAmount:N2}");
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────────");
            sb.AppendLine("ORDER STATUS");
            sb.AppendLine("─────────────────────────────────────────");
            sb.AppendLine($"Order Status: {transaction.Status}");
            sb.AppendLine($"Seller Response: {transaction.SellerStatus ?? "Pending"}");
            if (transaction.SellerResponseDate.HasValue)
            {
                sb.AppendLine($"Seller Response Date: {transaction.SellerResponseDate:yyyy-MM-dd HH:mm:ss}");
            }
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine("Thank you for your transaction!");
            sb.AppendLine("═══════════════════════════════════════════");

            return sb.ToString();
        }
    }
}