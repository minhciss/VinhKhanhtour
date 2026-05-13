using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanhCMS.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioDataToPoiTranslation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "AudioData",
                table: "PoiTranslations",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
