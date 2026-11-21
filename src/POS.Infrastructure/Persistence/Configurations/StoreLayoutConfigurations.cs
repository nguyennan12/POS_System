using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.StoreLayouts;
using POS.Domain.StoreLayouts.Picking;

namespace POS.Infrastructure.Persistence.Configurations;

public class StoreLayoutConfiguration : IEntityTypeConfiguration<StoreLayout>
{
    public void Configure(EntityTypeBuilder<StoreLayout> builder)
    {
        builder.ToTable("store_layouts");
        builder.ConfigureUuidPrimaryKey();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Revision).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("TIMEZONE('utc', now())");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("TIMEZONE('utc', now())");

        builder.HasIndex(x => new { x.StoreId, x.VersionNumber }).IsUnique();

        builder.HasMany(x => x.Zones)
            .WithOne(z => z.Layout)
            .HasForeignKey(z => z.LayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Cells)
            .WithOne(c => c.Layout)
            .HasForeignKey(c => c.LayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Shelves)
            .WithOne(s => s.Layout)
            .HasForeignKey(s => s.LayoutId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StoreZoneConfiguration : IEntityTypeConfiguration<StoreZone>
{
    public void Configure(EntityTypeBuilder<StoreZone> builder)
    {
        builder.ToTable("store_zones");
        builder.ConfigureUuidPrimaryKey();

        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Color).IsRequired().HasMaxLength(20).HasDefaultValue("#3B82F6");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("TIMEZONE('utc', now())");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("TIMEZONE('utc', now())");

        builder.HasIndex(x => new { x.LayoutId, x.Code }).IsUnique();
    }
}

public class StoreLayoutCellConfiguration : IEntityTypeConfiguration<StoreLayoutCell>
{
    public void Configure(EntityTypeBuilder<StoreLayoutCell> builder)
    {
        builder.ToTable("store_layout_cells");
        builder.ConfigureUuidPrimaryKey();

        builder.Property(x => x.CellType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.TraversalCost).HasDefaultValue(1);

        builder.HasIndex(x => new { x.LayoutId, x.X, x.Y }).IsUnique();
    }
}

public class ShelfConfiguration : IEntityTypeConfiguration<Shelf>
{
    public void Configure(EntityTypeBuilder<Shelf> builder)
    {
        builder.ToTable("shelves");
        builder.ConfigureUuidPrimaryKey();

        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Orientation).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("TIMEZONE('utc', now())");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("TIMEZONE('utc', now())");

        builder.HasIndex(x => new { x.LayoutId, x.Code }).IsUnique();

        builder.HasOne(x => x.Zone)
            .WithMany()
            .HasForeignKey(x => x.ZoneId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Levels)
            .WithOne(l => l.Shelf)
            .HasForeignKey(l => l.ShelfId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShelfLevelConfiguration : IEntityTypeConfiguration<ShelfLevel>
{
    public void Configure(EntityTypeBuilder<ShelfLevel> builder)
    {
        builder.ToTable("shelf_levels");
        builder.ConfigureUuidPrimaryKey();

        builder.Property(x => x.Label).HasMaxLength(100);

        builder.HasIndex(x => new { x.ShelfId, x.LevelNumber }).IsUnique();

        builder.HasMany(x => x.Placements)
            .WithOne(p => p.ShelfLevel)
            .HasForeignKey(p => p.ShelfLevelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductPlacementConfiguration : IEntityTypeConfiguration<ProductPlacement>
{
    public void Configure(EntityTypeBuilder<ProductPlacement> builder)
    {
        builder.ToTable("product_placements");
        builder.ConfigureUuidPrimaryKey();

        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("TIMEZONE('utc', now())");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("TIMEZONE('utc', now())");

        builder.HasIndex(x => new { x.ShelfLevelId, x.SkuId }).IsUnique();
        builder.HasIndex(x => x.SkuId);
    }
}

public class ProductPlacementHistoryConfiguration : IEntityTypeConfiguration<ProductPlacementHistory>
{
    public void Configure(EntityTypeBuilder<ProductPlacementHistory> builder)
    {
        builder.ToTable("product_placement_histories");
        builder.ConfigureUuidPrimaryKey();

        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("TIMEZONE('utc', now())");

        builder.HasIndex(x => new { x.LayoutId, x.SkuId });
    }
}

public class PickingOrderConfiguration : IEntityTypeConfiguration<PickingOrder>
{
    public void Configure(EntityTypeBuilder<PickingOrder> builder)
    {
        builder.ToTable("picking_orders");
        builder.ConfigureUuidPrimaryKey();

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("TIMEZONE('utc', now())");

        builder.HasIndex(x => new { x.StoreId, x.Status, x.CreatedAt });

        builder.HasMany(x => x.Items)
            .WithOne(i => i.PickingOrder)
            .HasForeignKey(i => i.PickingOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PickingOrderItemConfiguration : IEntityTypeConfiguration<PickingOrderItem>
{
    public void Configure(EntityTypeBuilder<PickingOrderItem> builder)
    {
        builder.ToTable("picking_order_items");
        builder.ConfigureUuidPrimaryKey();

        builder.Property(x => x.RequestedQty).HasPrecision(18, 3);
        builder.Property(x => x.PickedQty).HasPrecision(18, 3);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ShelfCodeSnapshot).HasMaxLength(50);
        builder.Property(x => x.ZoneNameSnapshot).HasMaxLength(200);
        builder.Property(x => x.Note).HasMaxLength(500);
    }
}
