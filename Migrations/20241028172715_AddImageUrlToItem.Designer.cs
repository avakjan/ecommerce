﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OnlineShoppingSite.Models;

#nullable disable

namespace OnlineShoppingSite.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241028172715_AddImageUrlToItem")]
    partial class AddImageUrlToItem
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.10");

            modelBuilder.Entity("OnlineShoppingSite.Models.Cart", b =>
                {
                    b.Property<int>("CartId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.HasKey("CartId");

                    b.ToTable("Carts");
                });

            modelBuilder.Entity("OnlineShoppingSite.Models.Item", b =>
                {
                    b.Property<int>("ItemId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Price")
                        .HasColumnType("TEXT");

                    b.HasKey("ItemId");

                    b.ToTable("Items");

                    b.HasData(
                        new
                        {
                            ItemId = 1,
                            Description = "Description for Item 1",
                            ImageUrl = "https://example.com/image1.jpg",
                            Name = "Item 1",
                            Price = 9.99m
                        },
                        new
                        {
                            ItemId = 2,
                            Description = "Description for Item 2",
                            ImageUrl = "https://example.com/image2.jpg",
                            Name = "Item 2",
                            Price = 19.99m
                        },
                        new
                        {
                            ItemId = 3,
                            Description = "Description for Item 3",
                            ImageUrl = "https://example.com/image3.jpg",
                            Name = "Item 3",
                            Price = 29.99m
                        });
                });
#pragma warning restore 612, 618
        }
    }
}