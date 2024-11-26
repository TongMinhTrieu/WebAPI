using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreateStoreProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tạo stored procedure
            migrationBuilder.Sql(@"
            CREATE PROCEDURE GetMovieID @id
            AS
            BEGIN
                SELECT * FROM Movies Where ID = @id;
            END
        ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa stored procedure nếu cần
            migrationBuilder.Sql("DROP PROCEDURE GetMovieID");
        }
    }
}
