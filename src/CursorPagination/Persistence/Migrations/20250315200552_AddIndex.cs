using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CursorPagination.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"TRUNCATE TABLE cursor_pagination.users");
            
            //migrationBuilder.Sql(@"DROP INDEX idx_users_surname_name;");
            //migrationBuilder.Sql(
            //    @"CREATE INDEX idx_users_surname_name  ON cursor_pagination.users (""Surname"" DESC, ""Name"" ASC);");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        { 
          
        }
    }
}
