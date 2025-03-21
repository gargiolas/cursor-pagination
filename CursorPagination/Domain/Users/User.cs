namespace CursorPagination.Domain.Users;

internal class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Email { get; set; }
    public string? Address { get; set; }
}