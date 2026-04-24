using System.ComponentModel.DataAnnotations;

namespace Lasamify.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? ProfilePicturePath { get; set; }

        public string Role { get; set; } = "Buyer"; // Buyer or Seller

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Transaction> BuyerTransactions { get; set; } = new List<Transaction>();
    }

    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        public string? ImagePath { get; set; }

        public string Category { get; set; } = "General";

        public int Stock { get; set; } = 1;

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int SellerId { get; set; }
        public User? Seller { get; set; }

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public class Transaction
    {
        public int Id { get; set; }

        public int BuyerId { get; set; }
        public User? Buyer { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int Quantity { get; set; } = 1;

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Completed, Cancelled

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    }
}
