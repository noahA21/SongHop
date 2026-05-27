using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SongHop.Data.Migrations
{
    /// <inheritdoc />
    public partial class SwapToLastFm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SpotifyId",
                table: "Nodes",
                newName: "ExternalId");

            migrationBuilder.RenameIndex(
                name: "IX_Nodes_SpotifyId",
                table: "Nodes",
                newName: "IX_Nodes_ExternalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExternalId",
                table: "Nodes",
                newName: "SpotifyId");

            migrationBuilder.RenameIndex(
                name: "IX_Nodes_ExternalId",
                table: "Nodes",
                newName: "IX_Nodes_SpotifyId");
        }
    }
}
