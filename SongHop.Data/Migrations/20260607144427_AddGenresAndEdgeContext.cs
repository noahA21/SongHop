using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SongHop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGenresAndEdgeContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Edges_SourceId_TargetId",
                table: "Edges");

            migrationBuilder.AddColumn<string>(
                name: "Genres",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContextRole",
                table: "Edges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContextTitle",
                table: "Edges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContextYear",
                table: "Edges",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Edges_SourceId_TargetId_Type_ContextTitle",
                table: "Edges",
                columns: new[] { "SourceId", "TargetId", "Type", "ContextTitle" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Edges_SourceId_TargetId_Type_ContextTitle",
                table: "Edges");

            migrationBuilder.DropColumn(
                name: "Genres",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ContextRole",
                table: "Edges");

            migrationBuilder.DropColumn(
                name: "ContextTitle",
                table: "Edges");

            migrationBuilder.DropColumn(
                name: "ContextYear",
                table: "Edges");

            migrationBuilder.CreateIndex(
                name: "IX_Edges_SourceId_TargetId",
                table: "Edges",
                columns: new[] { "SourceId", "TargetId" },
                unique: true);
        }
    }
}
