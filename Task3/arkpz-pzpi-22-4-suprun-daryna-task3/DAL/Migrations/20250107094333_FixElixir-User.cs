using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixElixirUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Elixirs_AspNetUsers_AppUserId",
                table: "Elixirs");

            migrationBuilder.DropIndex(
                name: "IX_Elixirs_AppUserId",
                table: "Elixirs");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Elixirs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "Elixirs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Elixirs_AppUserId",
                table: "Elixirs",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Elixirs_AspNetUsers_AppUserId",
                table: "Elixirs",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
