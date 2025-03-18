App

Bozza di codice da integrare

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public static class QueryableExtensions
{
    public static async Task<List<TOut>> ToRankedListAsync<T, TOut>(
        this DbContext context, 
        string tableName, 
        List<(string ColumnName, bool Ascending)> orderByColumns, 
        List<string> selectColumns, 
        Func<RankedEntity<T>, TOut> selector)
        where T : class
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentException("Il nome della tabella non può essere vuoto.");
        if (orderByColumns == null || !orderByColumns.Any()) throw new ArgumentException("Devi specificare almeno una colonna per l'ordinamento.");
        if (selectColumns == null || !selectColumns.Any()) throw new ArgumentException("Devi specificare almeno una colonna per la selezione.");

        // Costruisce la parte ORDER BY dinamicamente
        var orderByClause = string.Join(", ", orderByColumns.Select(o => $"{o.ColumnName} {(o.Ascending ? "ASC" : "DESC")}"));

        // Costruisce la parte SELECT dinamicamente
        var selectClause = string.Join(", ", selectColumns);

        // La query SQL con l'ORDER BY e il SELECT dinamico
        var query = $@"
            SELECT ROW_NUMBER() OVER (ORDER BY {orderByClause}) AS Rank, {selectClause} 
            FROM {tableName}";

        // Esegui la query SQL
        var rankedResults = await context.Set<T>()
            .FromSqlRaw(query)
            .Select(e => new RankedEntity<T> 
            { 
                Rank = EF.Property<int>(e, "Rank"), 
                Entity = e 
            })
            .ToListAsync();

        // Proietta solo le colonne specifiche con il Rank
        return rankedResults.Select(r => selector(r)).ToList();
    }
}

// Classe per gestire il ranking
public class RankedEntity<T>
{
    public int Rank { get; set; }
    public T Entity { get; set; }
}