using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace OnlineShoppingSite.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ShippingDetails> ShippingDetails { get; set; }
        public DbSet<Category> Categories { get; set; }


        // Configure entity relationships and keys
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<CartItem>();

            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "T-Shirts" },
                new Category { CategoryId = 2, Name = "Hoodies" },
                new Category { CategoryId = 3, Name = "Accessories" }
            );

            modelBuilder.Entity<Item>().HasData(
                new Item { ItemId = 1, Name = "Basic T-Shirt", Price = 9.99M, Description = "A basic t-shirt.", ImageUrl = "https://pngimg.com/uploads/tshirt/tshirt_PNG5435.png", CategoryId = 1 },
                new Item { ItemId = 2, Name = "Cool Hoodie", Price = 19.99M, Description = "A cool hoodie.", ImageUrl = "https://static.vecteezy.com/system/resources/previews/034/969/304/large_2x/ai-generated-t-shirt-mockup-clip-art-free-png.png", CategoryId = 1 },
                new Item { ItemId = 3, Name = "Stylish Cap", Price = 29.99M, Description = "A stylish cap.", ImageUrl = "https://www.racerworldwide.net/cdn/shop/files/front_white_1_31a53b32-c70b-48ef-8612-d869fc6d5877_750x.jpg?v=1723733410", CategoryId = 2 }
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