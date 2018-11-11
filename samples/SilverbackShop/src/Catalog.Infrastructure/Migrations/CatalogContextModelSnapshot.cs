﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using SilverbackShop.Catalog.Domain.Model;
using SilverbackShop.Catalog.Infrastructure;
using System;

namespace SilverbackShop.Catalog.Infrastructure.Migrations
{
    [DbContext(typeof(CatalogDbContext))]
    partial class CatalogContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.3-rtm-10026");

            modelBuilder.Entity("SilverbackShop.Catalog.Domain.Model.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description")
                        .IsRequired();

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(300);

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.Property<int>("Status");

                    b.Property<decimal>("UnitPrice");

                    b.HasKey("Id");

                    b.HasAlternateKey("SKU");

                    b.ToTable("Products");
                });
#pragma warning restore 612, 618
        }
    }
}
