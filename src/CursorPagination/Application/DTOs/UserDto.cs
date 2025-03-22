namespace CursorPagination.Application.DTOs;

internal sealed class UserDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Surname { get; init; }
    public required string Email { get; init; }
}