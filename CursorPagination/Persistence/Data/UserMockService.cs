using Bogus;
using CursorPagination.Domain.Users;

namespace CursorPagination.Persistence.Data;

internal static class UserMockService
{
    internal static List<User> GetUsers(int numberOfUsers)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numberOfUsers);

        var userFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => Guid.CreateVersion7()) // Version 7 Guid
            .RuleFor(u => u.Name, f => f.Name.FirstName()) // Random First Name
            .RuleFor(u => u.Surname, f => f.Name.LastName()) // Random Last Name
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name, u.Surname)) // Email based on name
            .RuleFor(u => u.Address, f => f.Address.FullAddress()); // Random Address

        // Generate a single fake User
        return userFaker.Generate(numberOfUsers);
    }
}