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

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "T-Shirts" },
                new Category { CategoryId = 2, Name = "Hoodies" },
                new Category { CategoryId = 3, Name = "Accessories" },
                new Category { CategoryId = 4, Name = "Footwear" }
            );

            // Seed Items
            modelBuilder.Entity<Item>().HasData(
                new Item
                {
                    ItemId = 1,
                    Name = "Garage T-shirt",
                    Price = 60M,
                    Description = "T-shirt in washed black.",
                    ImageUrl = "https://www.racerworldwide.net/cdn/shop/files/PrintPanelTFrontFFFFFF_750x.jpg?v=1723727728",
                    CategoryId = 1
                },
                new Item
                {
                    ItemId = 2,
                    Name = "Track Hoodie",
                    Price = 130M,
                    Description = "Oversized hoodie with distressed stripes.",
                    ImageUrl = "https://www.racerworldwide.net/cdn/shop/files/front_white_1_31a53b32-c70b-48ef-8612-d869fc6d5877_750x.jpg?v=1723733410",
                    CategoryId = 2
                },
                new Item
                {
                    ItemId = 3,
                    Name = "Glitch Leo Beanie",
                    Price = 55M,
                    Description = "Cotton beanie with all-over print.",
                    ImageUrl = "https://www.racerworldwide.net/cdn/shop/files/FW24_Glitch_Beanie_Camo_LB_FF_1_750x.jpg?v=1726757323",
                    CategoryId = 3
                },
                new Item
                {
                    ItemId = 4,
                    Name = "Vibram® Desert Boots",
                    Price = 240M,
                    Description = "Racer Suede Boots with Vibram® outsole.",
                    ImageUrl = "https://www.racerworldwide.net/cdn/shop/files/SuedeRightSideFFFFFF_1_750x.jpg?v=1723730360",
                    CategoryId = 4
                }
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