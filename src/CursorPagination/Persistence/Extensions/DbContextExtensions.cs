using System.Reflection;
using System.Text;
using CursorPagination.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CursorPagination.Persistence.Extensions;

/// <summary>
/// Provides extension methods for <see cref="DbContext"/> to facilitate advanced querying capabilities,
/// including ranked pagination and dynamic query customization.
/// </summary>
internal static class DbContextExtensions
{
    /// Generates a ranked, paginated query based on the specified parameters, using dynamic column selection and ordering.
    /// <typeparam name="TEntity">
    /// The type of the entity associated with the table being queried.
    /// </typeparam>
    /// <param name="context">
    /// The DbContext instance used to interact with the database.
    /// </param>
    /// <param name="tableName">
    /// The name of the database table to query.
    /// </param>
    /// <param name="columnsToReturn">
    /// A set of column names to include in the query results.
    /// </param>
    /// <param name="orderColumns">
    /// A set of tuples specifying the column names for ordering and their respective sorting direction (ascending or descending).
    /// </param>
    /// <param name="startIndex">
    /// The zero-based index of the first record to fetch. Defaults to 0.
    /// </param>
    /// <param name="recordForPage">
    /// The maximum number of records to retrieve. Defaults to 10.
    /// </param>
    /// <returns>
    /// An IQueryable representing the ranked and paginated results of the query.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if no columns are specified for selection or ordering.
    /// </exception>
    public static IQueryable<TEntity> ToQuerableRanked<TEntity>(
        this DbContext context,
        string tableName,
        HashSet<string> columnsToReturn,
        HashSet<(string column, bool ascending)> orderColumns,
        int startIndex = 0,
        int recordForPage = 10) where TEntity : class
    {
        if (orderColumns == null || !orderColumns.Any())
            throw new ArgumentException("At least one column must be provided for ordering.");

        if (columnsToReturn == null || columnsToReturn.Count == 0)
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
                      SELECT {columnsClause},
                             "RowIndex"
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

    /// Executes a ranked database query with dynamic column selection and pagination, returning the results as a list of transformed entities.
    /// <typeparam name="TEntity">
    /// The type of the entity being queried in the database.
    /// </typeparam>
    /// <typeparam name="TOut">
    /// The type of the object to be returned after applying the selector transformation.
    /// </typeparam>
    /// <param name="context">
    /// The DbContext instance used to interact with the database.
    /// </param>
    /// <param name="tableName">
    /// The name of the database table to query.
    /// </param>
    /// <param name="columnsToReturn">
    /// A set of column names to include in the query results.
    /// </param>
    /// <param name="orderColumns">
    /// A set of tuples specifying the column names for ordering and their respective sorting direction (ascending or descending).
    /// </param>
    /// <param name="selector">
    /// A function used to transform each ranked entity into the desired output type.
    /// </param>
    /// <param name="startIndex">
    /// The zero-based index of the first record to fetch. Defaults to 0.
    /// </param>
    /// <param name="recordForPage">
    /// The maximum number of records to retrieve. Defaults to 10.
    /// </param>
    /// <param name="cancellationToken">
    /// A CancellationToken to observe while waiting for the asynchronous operation to complete. Defaults to none.
    /// </param>
    /// <returns>
    /// A Task representing the asynchronous operation, containing a list of transformed results based on the ranked query.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if no columns are specified for selection or ordering.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the selector function is null.
    /// </exception>
    public static async Task<List<TOut>> ToRankedListAsync<TEntity, TOut>(
        this DbContext context,
        string tableName,
        HashSet<string> columnsToReturn,
        HashSet<(string column, bool ascending)> orderColumns,
        Func<RankedEntity<TEntity>, TOut> selector,
        int startIndex = 0,
        int recordForPage = 10,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        if (orderColumns == null || !orderColumns.Any())
            throw new ArgumentException("At least one column must be provided for ordering.");

        if (columnsToReturn == null || !columnsToReturn.Any())
            throw new ArgumentException("At least one column must be provided for selection.");

        ArgumentNullException.ThrowIfNull(selector);

        ValidateColumnsForEntity<TEntity>(columnsToReturn);

        DisplayEntityProperties<TEntity>(context);

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
                   SELECT {columnsClause},
                          "RowIndex"
                   FROM (
                   SELECT {columnsClause}, 
                          ROW_NUMBER() 
                              OVER (ORDER BY {orderByClause}) AS "RowIndex"
                   FROM {tableName})
                   WHERE "RowIndex" >= {startIndex} 
                    LIMIT {recordForPage + 1}
                   """;

        var rankedResults = await context.Set<TEntity>()
            .FromSqlRaw(sql)
            .Select(e => new RankedEntity<TEntity>
            {
                Rank = EF.Property<long>(e, "RowIndex"),
                Entity = e
            })
            .ToListAsync(cancellationToken);

        // Proietta solo le colonne specifiche con il Rank
        return rankedResults.Select(r => selector(r)).ToList();
    }


    /// Retrieves a ranked and paginated list of DTO entities from the specified database table based on the provided parameters.
    /// <typeparam name="TEntity">
    /// The type of the entity to be returned, representing the DTO structure.
    /// </typeparam>
    /// <param name="context">
    /// The DbContext instance used to interact with the database.
    /// </param>
    /// <param name="tableName">
    /// The name of the database table to query.
    /// </param>
    /// <param name="orderColumns">
    /// A set of tuples specifying the column names for ordering and their respective sorting direction (ascending or descending).
    /// </param>
    /// <param name="startIndex">
    /// The zero-based index of the first record to fetch. Defaults to 0.
    /// </param>
    /// <param name="recordForPage">
    /// The maximum number of records to retrieve. Defaults to 10.
    /// </param>
    /// <param name="cancellationToken">
    /// A CancellationToken instance to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The result contains a list of DTO entities matching the query criteria.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if no columns are specified for selection or ordering, or if the provided order columns do not match any selectable columns.
    /// </exception>
    public static async Task<List<TEntity>> ToDtoRankedListAsync<TEntity>(
        this DbContext context,
        string tableName,
        HashSet<(string column, bool ascending)> orderColumns,
        int startIndex = 0,
        int recordForPage = 10,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        if (orderColumns == null || !orderColumns.Any())
            throw new ArgumentException("At least one column must be provided for ordering.");

        var columnsToReturn = GetColumnNames<TEntity>();

        if (columnsToReturn == null || columnsToReturn.Count == 0)
            throw new ArgumentException("At least one column must be provided for selection.");

        var exists = OrderParameterExists(orderColumns, columnsToReturn);
        if (!exists)
            throw new ArgumentException("At least one column must be provided for selection.");

        // Costruire la parte di ordinamento della query
        var orderByClause = new StringBuilder();
        foreach (var (column, ascending) in orderColumns)
        {
            if (orderByClause.Length > 0)
                orderByClause.Append(", ");
            orderByClause.Append($"\"{column}\" {(ascending ? "ASC" : "DESC")}");
        }

        // Costruire la parte di selezione dinamica
        var columnsClause = string.Join(", ",
            columnsToReturn.
                Where(item => !item.Equals("RowIndex")).
                Select(item => string.Concat("\"", item, "\"")));

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

        return
            await context.Database.SqlQueryRaw<TEntity>(sql)
                .ToListAsync(cancellationToken);
    }

    /// Checks whether all the columns specified in the order parameters exist in the list of columns to return.
    /// <param name="orderColumns">
    /// A set of tuples containing column names and their sorting directions (ascending or descending) to be used for ordering.
    /// </param>
    /// <param name="columnsToReturn">
    /// A list of column names that are included in the query results.
    /// </param>
    /// <returns>
    /// True if all columns specified in the order parameters are present in the list of columns to return; otherwise, false.
    /// </returns>
    private static bool OrderParameterExists(HashSet<(string column, bool ascending)> orderColumns,
        List<string> columnsToReturn)
    {
        return orderColumns.Select(item => item.column).Intersect(columnsToReturn).Count() == orderColumns.Count;
    }

    /// Executes a ranked and paginated query against a database table, returning results as an IQueryable of TEntity.
    /// <typeparam name="TEntity">
    /// The type of the entity that maps to the records in the target database table.
    /// </typeparam>
    /// <param name="context">
    /// The DbContext instance used for interacting with the database.
    /// </param>
    /// <param name="tableName">
    /// The name of the database table to query.
    /// </param>
    /// <param name="columnsToReturn">
    /// A set of column names to include in the query results.
    /// </param>
    /// <param name="orderColumns">
    /// A set of tuples specifying column names for ordering and their respective sorting directions (ascending or descending).
    /// </param>
    /// <param name="startIndex">
    /// The zero-based index of the first record to fetch. Defaults to 0.
    /// </param>
    /// <param name="recordForPage">
    /// The maximum number of records to retrieve in one query. Defaults to 10.
    /// </param>
    /// <param name="cancellationToken">
    /// A CancellationToken instance to observe while executing the query. Defaults to CancellationToken.None.
    /// </param>
    /// <returns>
    /// An IQueryable representing the ranked and paginated results of the query.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if no columns are specified for selection or ordering.
    /// </exception>
    public static IQueryable<TEntity> ToDtoRankedQuerableAsync<TEntity>(
        this DbContext context,
        string tableName,
        HashSet<string> columnsToReturn,
        HashSet<(string column, bool ascending)> orderColumns,
        int startIndex = 0,
        int recordForPage = 10,
        CancellationToken cancellationToken = default) where TEntity : class
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

        var sql = $"""
                   SELECT {columnsClause},
                          "RowIndex"
                   FROM (
                   SELECT {columnsClause}, 
                          ROW_NUMBER() 
                              OVER (ORDER BY {orderByClause}) AS "RowIndex"
                   FROM {tableName})
                   WHERE "RowIndex" >= {startIndex} 
                    LIMIT {recordForPage + 1}
                   """;

        return context.Database.SqlQueryRaw<TEntity>(sql).AsQueryable();
    }

    /// Displays the properties of the specified entity type within the given DbContext instance.
    /// Outputs details such as the names of the properties and whether they are shadow properties.
    /// <typeparam name="TEntity">
    /// The type of the entity whose properties are being examined.
    /// </typeparam>
    /// <param name="context">
    /// The DbContext instance that contains the entity type being inspected.
    /// </param>
    private static void DisplayEntityProperties<TEntity>(DbContext context) where TEntity : class
    {
        var contextEntity = context.Model.FindEntityType(typeof(TEntity));
        var properties = contextEntity.GetProperties();

        foreach (var property in properties)
        {
            Console.WriteLine($"Property: {property.Name}, Shadow: {property.IsShadowProperty()}");
        }
    }


    /// Validates that all specified column names in the provided set exist as properties in the given entity type.
    /// <typeparam name="TEntity">
    /// The entity type to validate against. Must be a class.
    /// </typeparam>
    /// <param name="columnsToReturn">
    /// A set of column names to validate against the entity type's properties.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if any specified column names do not correspond to properties of the entity type.
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

    /// Retrieves a list of column names representing the properties of the specified entity type.
    /// <typeparam name="TEntity">
    /// The type of the entity whose properties will be inspected to determine the column names.
    /// </typeparam>
    /// <returns>
    /// A list of strings representing the names of public instance properties of the specified entity type.
    /// </returns>
    private static List<string> GetColumnNames<TEntity>() where TEntity : class
    {
        // Verifica che tutte le colonne di "columnsToReturn" siano proprietà di T
        var properties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return properties.ToList();
    }
}