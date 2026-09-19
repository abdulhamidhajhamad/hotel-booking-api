using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FilterCityUniqueIndexByIsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cities_Name_Country",
                table: "Cities");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Name_Country",
                table: "Cities",
                columns: new[] { "Name", "Country" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cities_Name_Country",
                table: "Cities");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Name_Country",
                table: "Cities",
                columns: new[] { "Name", "Country" },
                unique: true);
        }
    }
}
