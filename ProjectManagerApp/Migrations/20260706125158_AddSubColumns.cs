using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagerApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSubColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentColumnId",
                table: "Columns",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Columns_ParentColumnId",
                table: "Columns",
                column: "ParentColumnId");

            migrationBuilder.AddForeignKey(
                name: "FK_Columns_Columns_ParentColumnId",
                table: "Columns",
                column: "ParentColumnId",
                principalTable: "Columns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Columns_Columns_ParentColumnId",
                table: "Columns");

            migrationBuilder.DropIndex(
                name: "IX_Columns_ParentColumnId",
                table: "Columns");

            migrationBuilder.DropColumn(
                name: "ParentColumnId",
                table: "Columns");
        }
    }
}
