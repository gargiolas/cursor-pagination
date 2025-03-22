using CursorPagination.Application.Abstractions.Services;

namespace CursorPagination.Domain.Abstractions;

public class EntityEqualityComparer<TEntity> : IEqualityComparer<TEntity>
    where TEntity : class
{
    public bool Equals(TEntity? x, TEntity? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;
        
        // Recupera tutte le proprietà pubbliche del tipo TEntity
        var properties = typeof(TEntity).GetProperties();

        foreach (var property in properties)
        {
            // Ottieni i valori delle due proprietà
            var valueX = property.GetValue(x);
            var valueY = property.GetValue(y);

            // Confronta i valori
            if (!Equals(valueX, valueY))
            {
                return false;
            }
        }

        return true; // Tutte le proprietà corrispondono
   
    }

    public int GetHashCode(TEntity? obj)
    {
        if (obj is null) return 0;

        // Recupera tutte le proprietà pubbliche del tipo TEntity
        var properties = typeof(TEntity).GetProperties();

        int hash = 17;

        foreach (var property in properties)
        {
            var value = property.GetValue(obj);
            hash = hash * 23 + (value?.GetHashCode() ?? 0); // Usa GetHashCode per generare l'hash
        }

        return hash;
    }

}

public class EntityFilterEqualityComparer<TEntity> : IEqualityComparer<TEntity>
    where TEntity : ICursorFilter
{
    public bool Equals(TEntity? x, TEntity? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;
        
        // Recupera tutte le proprietà pubbliche del tipo TEntity
        var properties = typeof(TEntity).GetProperties();

        foreach (var property in properties)
        {
            // Ottieni i valori delle due proprietà
            var valueX = property.GetValue(x);
            var valueY = property.GetValue(y);

            // Confronta i valori
            if (!Equals(valueX, valueY))
            {
                return false;
            }
        }

        return true; // Tutte le proprietà corrispondono
   
    }

    public int GetHashCode(TEntity? obj)
    {
        if (obj is null) return 0;

        // Recupera tutte le proprietà pubbliche del tipo TEntity
        var properties = typeof(TEntity).GetProperties();

        int hash = 17;

        foreach (var property in properties)
        {
            var value = property.GetValue(obj);
            hash = hash * 23 + (value?.GetHashCode() ?? 0); // Usa GetHashCode per generare l'hash
        }

        return hash;
    }

}