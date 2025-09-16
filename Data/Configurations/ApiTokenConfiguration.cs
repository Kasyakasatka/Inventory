using InventoryManagement.Web.Constants;
using InventoryManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Web.Data.Configurations;

public class ApiTokenConfiguration : IEntityTypeConfiguration<ApiToken>
{
    public void Configure(EntityTypeBuilder<ApiToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(ValidationConstants.TokenMaxLength);

        builder.HasIndex(t => t.Token).IsUnique();

        builder.Property(t => t.InventoryId).IsRequired();

        builder.HasOne(t => t.Inventory)
            .WithMany()
            .HasForeignKey(t => t.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}