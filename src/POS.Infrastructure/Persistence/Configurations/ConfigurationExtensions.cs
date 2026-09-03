using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Common;

namespace POS.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    public static void ConfigureUuidPrimaryKey<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : BaseEntity
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
    }

    public static PropertyBuilder<decimal> HasMoneyPrecision(this PropertyBuilder<decimal> builder) =>
        builder.HasPrecision(18, 2);

    public static PropertyBuilder<decimal?> HasMoneyPrecision(this PropertyBuilder<decimal?> builder) =>
        builder.HasPrecision(18, 2);

    public static PropertyBuilder<decimal> HasQuantityPrecision(this PropertyBuilder<decimal> builder) =>
        builder.HasPrecision(18, 3);

    public static PropertyBuilder<decimal> HasRatePrecision(this PropertyBuilder<decimal> builder) =>
        builder.HasPrecision(5, 4);

    public static EntityTypeBuilder<TEntity> HasTableCheckConstraint<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string name,
        string sql)
        where TEntity : class
    {
        builder.ToTable(table => table.HasCheckConstraint(name, sql));
        return builder;
    }
}
