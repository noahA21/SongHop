using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SongHop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGraphEdgesAndIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Edges_Nodes_SourceId",
                table: "Edges");

            migrationBuilder.DropForeignKey(
                name: "FK_Edges_Nodes_TargetId",
                table: "Edges");

            migrationBuilder.DropIndex(
                name: "IX_Nodes_Name",
                table: "Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Edges_SourceId_TargetId",
                table: "Edges");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Nodes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_SpotifyId",
                table: "Nodes",
                column: "SpotifyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Edges_SourceId_TargetId",
                table: "Edges",
                columns: new[] { "SourceId", "TargetId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Edges_Nodes_SourceId",
                table: "Edges",
                column: "SourceId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Edges_Nodes_TargetId",
                table: "Edges",
                column: "TargetId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Edges_Nodes_SourceId",
                table: "Edges");

            migrationBuilder.DropForeignKey(
                name: "FK_Edges_Nodes_TargetId",
                table: "Edges");

            migrationBuilder.DropIndex(
                name: "IX_Nodes_SpotifyId",
                table: "Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Edges_SourceId_TargetId",
                table: "Edges");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Nodes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_Name",
                table: "Nodes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Edges_SourceId_TargetId",
                table: "Edges",
                columns: new[] { "SourceId", "TargetId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Edges_Nodes_SourceId",
                table: "Edges",
                column: "SourceId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Edges_Nodes_TargetId",
                table: "Edges",
                column: "TargetId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
