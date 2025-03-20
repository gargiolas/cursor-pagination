using CursorPagination.Application.Abstractions.Services;

namespace CursorPagination.Application.Abstractions;

/// <summary>
/// Represents a cursor utilized in pagination for tracking position and metadata associated with a specific entity.
/// </summary>
/// <typeparam name="TEntity">The type of the entity related to the cursor.</typeparam>
/// <param name="LastId">The unique identifier of the most recent entity within the cursor context.</param>
/// <param name="Entity">The entity object corresponding to the current position in the paginated set.</param>
/// <param name="Position">The numerical position of the current entity relative to the dataset.</param>
public sealed record Cursor<TEntity>(Guid LastId, TEntity Entity, int Position) where TEntity : ICursorFilter;