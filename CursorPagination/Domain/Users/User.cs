﻿namespace CursorPagination.Domain.Users;

internal sealed class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Email { get; set; }
    public string? Address { get; set; }
}