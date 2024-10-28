using Microsoft.EntityFrameworkCore;

namespace OnlineShoppingSite.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items { get; set; }
        public DbSet<Cart> Carts { get; set; }
        //public DbSet<CartItem> CartItems { get; set; }

        // Configure entity relationships and keys
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<CartItem>();

            modelBuilder.Entity<Item>().HasData(
                new Item { ItemId = 1, Name = "Item 1", Price = 9.99M, Description = "Description for Item 1", ImageUrl = "https://example.com/image1.jpg" },
                new Item { ItemId = 2, Name = "Item 2", Price = 19.99M, Description = "Description for Item 2", ImageUrl = "https://example.com/image2.jpg" },
                new Item { ItemId = 3, Name = "Item 3", Price = 29.99M, Description = "Description for Item 3", ImageUrl = "https://example.com/image3.jpg" }
            );
        }

    }
}