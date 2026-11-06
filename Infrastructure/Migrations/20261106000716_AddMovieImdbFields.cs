using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieImdbFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImdbId",
                schema: "dbo",
                table: "Movies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ImdbRating",
                schema: "dbo",
                table: "Movies",
                type: "decimal(3,1)",
                precision: 3,
                scale: 1,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_Movies_ImdbId",
                schema: "dbo",
                table: "Movies",
                column: "ImdbId",
                unique: true,
                filter: "[ImdbId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Movies_ImdbRating",
                schema: "dbo",
                table: "Movies",
                sql: "[ImdbRating] >= 0 AND [ImdbRating] <= 10");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Movies_ImdbId",
                schema: "dbo",
                table: "Movies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Movies_ImdbRating",
                schema: "dbo",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "ImdbId",
                schema: "dbo",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "ImdbRating",
                schema: "dbo",
                table: "Movies");
        }
    }
}
