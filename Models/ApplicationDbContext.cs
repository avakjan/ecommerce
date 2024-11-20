// Models/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using OnlineShoppingSite.Models;

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
        public DbSet<Size> Sizes { get; set; }
        public DbSet<ItemSize> ItemSizes { get; set; }

        // Configure entity relationships and keys
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<CartItem>();

            // 1. Configure Composite Key for ItemSize
            modelBuilder.Entity<ItemSize>()
                .HasKey(isz => new { isz.ItemId, isz.SizeId });

            // 2. Configure Concurrency Token (Version)
            modelBuilder.Entity<ItemSize>()
                .Property(isz => isz.Version)
                .HasDefaultValue(0)
                .IsConcurrencyToken();

            // 3. Configure Relationships
            modelBuilder.Entity<ItemSize>()
                .HasOne(isz => isz.Item)
                .WithMany(i => i.ItemSizes)
                .HasForeignKey(isz => isz.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemSize>()
                .HasOne(isz => isz.Size)
                .WithMany(s => s.ItemSizes)
                .HasForeignKey(isz => isz.SizeId)
                .OnDelete(DeleteBehavior.Cascade);

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

            // Seed Sizes
            modelBuilder.Entity<Size>().HasData(
                // Clothing Sizes
                new Size { SizeId = 1, Name = "S" },
                new Size { SizeId = 2, Name = "M" },
                new Size { SizeId = 3, Name = "L" },
                new Size { SizeId = 4, Name = "XL" },
                // Footwear Sizes
                new Size { SizeId = 5, Name = "38" },
                new Size { SizeId = 6, Name = "39" },
                new Size { SizeId = 7, Name = "40" },
                new Size { SizeId = 8, Name = "41" },
                new Size { SizeId = 9, Name = "42" }
            );

            // Seed ItemSizes with Version
            modelBuilder.Entity<ItemSize>().HasData(
                // Garage T-shirt (ItemId = 1)
                new ItemSize { ItemId = 1, SizeId = 1, Quantity = 50, Version = 0 },
                new ItemSize { ItemId = 1, SizeId = 2, Quantity = 50, Version = 0 },
                new ItemSize { ItemId = 1, SizeId = 3, Quantity = 50, Version = 0 },
                new ItemSize { ItemId = 1, SizeId = 4, Quantity = 50, Version = 0 },

                // Track Hoodie (ItemId = 2)
                new ItemSize { ItemId = 2, SizeId = 1, Quantity = 30, Version = 0 },
                new ItemSize { ItemId = 2, SizeId = 2, Quantity = 30, Version = 0 },
                new ItemSize { ItemId = 2, SizeId = 3, Quantity = 30, Version = 0 },
                new ItemSize { ItemId = 2, SizeId = 4, Quantity = 30, Version = 0 },

                // Glitch Leo Beanie (ItemId = 3)
                new ItemSize { ItemId = 3, SizeId = 1, Quantity = 10, Version = 0 },
                new ItemSize { ItemId = 3, SizeId = 2, Quantity = 10, Version = 0 },
                new ItemSize { ItemId = 3, SizeId = 3, Quantity = 10, Version = 0 },
                new ItemSize { ItemId = 3, SizeId = 4, Quantity = 10, Version = 0 },

                // Vibram® Desert Boots (ItemId = 4)
                new ItemSize { ItemId = 4, SizeId = 5, Quantity = 20, Version = 0 },
                new ItemSize { ItemId = 4, SizeId = 6, Quantity = 20, Version = 0 },
                new ItemSize { ItemId = 4, SizeId = 7, Quantity = 20, Version = 0 },
                new ItemSize { ItemId = 4, SizeId = 8, Quantity = 20, Version = 0 },
                new ItemSize { ItemId = 4, SizeId = 9, Quantity = 20, Version = 0 }
            );

            // Configure other relationships
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Size)
                .WithMany()
                .HasForeignKey(oi => oi.SizeId);

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