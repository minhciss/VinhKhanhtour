using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanhCMS.Migrations
{
    /// <inheritdoc />
    public partial class AddImageDataToPoi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "Pois",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "Pois");
        }
    }
}
