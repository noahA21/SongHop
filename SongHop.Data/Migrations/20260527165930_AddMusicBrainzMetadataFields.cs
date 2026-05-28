using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SongHop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMusicBrainzMetadataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArtistType",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndYear",
                table: "Nodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartYear",
                table: "Nodes",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArtistType",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "EndYear",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "StartYear",
                table: "Nodes");
        }
    }
}
