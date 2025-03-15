using CursorPagination.Domain.Users;
using CursorPagination.Persistence.Data;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace CursorPagination.Persistence.Extensions;

internal static class MigrationExtensions
{
    internal static void ApplyMigration(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetService<ApplicationContext>();

        var pendingMigrations = context?.Database.GetPendingMigrations();
        if (pendingMigrations?.Any() != true) return;
        context?.Database.Migrate();
    }

    internal static void FillUserData(this IServiceProvider serviceProvider)
    {
        const int usersCount = 5000000;
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetService<ApplicationContext>();

        if (context is null || context.Set<User>().Any()) return;

        var users = UserMockService.GetUsers(usersCount);

        if (users.Count == 0) return;

        using var transaction = context?.Database.BeginTransaction();
        context!.BulkInsert(users);
        transaction!.Commit();
    }
}