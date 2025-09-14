﻿using InventoryManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InventoryManagement.Web.Constants;

namespace InventoryManagement.Web.Data.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(ValidationConstants.TitleMaxLength);
        builder.Property(i => i.Description)
            .HasMaxLength(ValidationConstants.DescriptionMaxLength);
        builder.Property(i => i.ImageUrl)
            .HasMaxLength(ValidationConstants.ImageUrlMaxLength);

        builder.Property(i => i.Tags)
            .HasColumnType("text[]");
        builder.Property(i => i.Version).IsRowVersion();

        builder.HasOne(i => i.Creator)
            .WithMany(u => u.Inventories)
            .HasForeignKey(i => i.CreatorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Category)
            .WithMany(c => c.Inventories)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => i.Tags);

        builder.Property(i => i.SearchVector)
           .HasColumnType("tsvector")
           .IsRequired(false); 

        builder.HasIndex(i => i.SearchVector)
            .HasMethod("GIST");
    }
}