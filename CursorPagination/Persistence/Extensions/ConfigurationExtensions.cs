using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CursorPagination.Persistence.Extensions;

internal static class ConfigurationExtensions
{
    internal static void SetAsShadowProperty<TEntity>(this EntityTypeBuilder<TEntity> builder, string propertyName)
     where TEntity : class
    {
        builder.Property<long>(propertyName);
    }
}