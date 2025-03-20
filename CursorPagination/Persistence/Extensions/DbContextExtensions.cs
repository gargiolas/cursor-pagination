using System.Reflection;
using System.Text;
using CursorPagination.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CursorPagination.Persistence.Extensions;

/// <summary>
/// Provides extension methods for <see cref="DbContext"/> to facilitate ranked pagination queries.
/// </summary>
internal static class DbContextExtensions
{
    /// Generates a ranked query with dynamic column selection and ordering, applying pagination.
    /// <typeparam name="TEntity">
    /// The entity type corresponding to the database table.
    /// </typeparam>
    /// <param name="context">
    /// The DbContext instance used to execute queries against the database.
    /// </param>
    /// <param name="tableName">
    /// The name of the database table to query.
    /// </param>
    /// <param name="columnsToReturn">
    /// A set of column names to include in the result set.
    /// </param>
    /// <param name="orderColumns">
    /// A set of tuples specifying the columns to order by and their respective sort directions (ascending or descending).
    /// </param>
    /// <param name="startIndex">
    /// The zero-based index of the first record to include in the result set. Defaults to 0.
    /// </param>
    /// <param name="recordForPage">
    /// The maximum number of records to retrieve per page. Defaults to 10.
    /// </param>
    /// <returns>
    /// An IQueryable representing the ranked, paginated result set of the query.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if no order columns or no columns to return are provided.
    /// </exception>
    public static IQueryable<TEntity> ToRanked<TEntity>(
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

    /// <summary>
    /// Converts the specified table in the DbContext into a paginated, ranked list of entities based on the given ordering and column selection criteria.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to query within the DbContext.</typeparam>
    /// <typeparam name="TOut">The type of the output after transforming the ranked entities using the specified selector.</typeparam>
    /// <param name="context">The instance of the DbContext used to query the database.</param>
    /// <param name="tableName">The name of the database table associated with the entity.</param>
    /// <param name="columnsToReturn">A set of columns to be selected in the result set.</param>
    /// <param name="orderColumns">A set of columns, along with their sorting orders, specifying how the results should be ranked.</param>
    /// <param name="selector">A function that maps a ranked entity to the desired output type.</param>
    /// <param name="startIndex">The index to start retrieving rows from. Default value is 0.</param>
    /// <param name="recordForPage">The maximum number of records to retrieve in a single call. Default value is 10.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of entities projected into the specified output type based on the provided rank.</returns>
    /// <exception cref="ArgumentException">Thrown when no columns are provided for ordering or when no columns are provided for selection.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the selector function is null.</exception>
    public static async Task<List<TOut>> ToRankedListAsync<TEntity, TOut>(
        this DbContext context,
        string tableName,
        HashSet<string> columnsToReturn,
        HashSet<(string column, bool ascending)> orderColumns,
        Func<RankedEntity<TEntity>, TOut> selector,
        int startIndex = 0,
        int recordForPage = 10) where TEntity : class
    {
        if (orderColumns == null || !orderColumns.Any())
            throw new ArgumentException("At least one column must be provided for ordering.");

        if (columnsToReturn == null || !columnsToReturn.Any())
            throw new ArgumentException("At least one column must be provided for selection.");

        ArgumentNullException.ThrowIfNull(selector);

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

        var sql = $"""
                   SELECT {columnsClause}
                   FROM (
                   SELECT {columnsClause}, 
                          ROW_NUMBER() 
                              OVER (ORDER BY {orderByClause}) AS "RowIndex"
                   FROM {tableName})
                   WHERE "RowIndex" > {startIndex} 
                    LIMIT {(recordForPage + 1)}
                   """;

        var rankedResults = await context.Set<TEntity>()
            .FromSqlRaw(sql)
            .Select(e => new RankedEntity<TEntity>
            {
                Rank = EF.Property<int>(e, "RowIndex"),
                Entity = e
            })
            .ToListAsync();

        // Proietta solo le colonne specifiche con il Rank
        return rankedResults.Select(r => selector(r)).ToList();
    }

    /// Validates that all columns specified in the set are valid properties of the entity type.
    /// Throws an exception if any invalid column names are detected.
    /// <typeparam name="TEntity">
    /// The entity type being validated. Must be a class.
    /// </typeparam>
    /// <param name="columnsToReturn">
    /// A set of column names to be validated as properties of the specified entity type.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if any of the given column names do not match a valid property of the entity type.
    /// </exception>
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