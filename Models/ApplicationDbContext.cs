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
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ShippingDetails> ShippingDetails { get; set; }


        // Configure entity relationships and keys
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<CartItem>();

            modelBuilder.Entity<Item>().HasData(
                new Item { ItemId = 1, Name = "Item 1", Price = 9.99M, Description = "Description for Item 1", ImageUrl = "https://pngimg.com/uploads/tshirt/tshirt_PNG5435.png" },
                new Item { ItemId = 2, Name = "Item 2", Price = 19.99M, Description = "Description for Item 2", ImageUrl = "https://static.vecteezy.com/system/resources/previews/034/969/304/large_2x/ai-generated-t-shirt-mockup-clip-art-free-png.png" },
                new Item { ItemId = 3, Name = "Item 3", Price = 29.99M, Description = "Description for Item 3", ImageUrl = "https://pics.clipartpng.com/Green_T_Shirt_PNG_Clip_Art-3106.png" }
            );

            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingDetails)
                .WithOne()
                .HasForeignKey<Order>(o => o.ShippingDetailsId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Item)
                .WithMany()
                .HasForeignKey(oi => oi.ItemId);
        }

    }
}