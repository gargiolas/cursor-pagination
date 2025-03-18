using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace CursorPagination.Persistence.Extensions;

internal static class DbContextExtensions
{
    public static IQueryable<TEntity> ToRank<TEntity>(
        this DbContext context,
        string tableName,
        HashSet<string> columnsToReturn,
        HashSet<(string column, bool ascending)> orderColumns,
        int startIndex = 0,
        int recordForPage = 10) where TEntity : class
    {
        if (orderColumns == null || !orderColumns.Any())
            throw new ArgumentException("At least one column must be provided for ordering.");

        if (columnsToReturn == null || !columnsToReturn.Any())
            throw new ArgumentException("At least one column must be provided for selection.");

        ValidateColumnsForEntity<TEntity>(columnsToReturn);

        // Costruire la parte di ordinamento della query
        var orderByClause = new StringBuilder();
        foreach (var (column, ascending) in orderColumns)
        {
            if (orderByClause.Length > 0)
                orderByClause.Append(", ");
            orderByClause.Append($"\"{column}\" {(ascending ? "ASC" : "DESC")}");
        }

        // Costruire la parte di selezione dinamica
        var columnsClause = string.Join(", ", columnsToReturn.Select(item => string.Concat("\"", item, "\"")));

        string sql = $"""
                   SELECT {columnsClause}
                   FROM (
                   SELECT {columnsClause}, 
                          ROW_NUMBER() 
                              OVER (ORDER BY {orderByClause}) AS "RowIndex"
                   FROM {tableName})
                   WHERE "RowIndex" > {startIndex} 
                    LIMIT {(recordForPage + 1)}
                   """;

        return context.Set<TEntity>().FromSqlRaw(sql);
    }

    private static void ValidateColumnsForEntity<TEntity>(HashSet<string> columnsToReturn) where TEntity : class
    {
        // Verifica che tutte le colonne di "columnsToReturn" siano proprietà di T
        var properties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var invalidColumns = columnsToReturn.Where(column => !properties.Contains(column)).ToList();
        if (invalidColumns.Count != 0)
        {
            throw new ArgumentException(
                $"The following columns are not valid properties of {typeof(TEntity).Name}: {string.Join(", ", invalidColumns)}");
        }
    }
}