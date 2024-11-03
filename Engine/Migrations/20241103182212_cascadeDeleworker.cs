using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engine.Migrations
{
    /// <inheritdoc />
    public partial class cascadeDeleworker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkerEvents_Workers_WorkerEntityWorkerId",
                table: "WorkerEvents");

            migrationBuilder.DropIndex(
                name: "IX_WorkerEvents_WorkerEntityWorkerId",
                table: "WorkerEvents");

            migrationBuilder.DropColumn(
                name: "WorkerEntityWorkerId",
                table: "WorkerEvents");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerEvents_WorkerId",
                table: "WorkerEvents",
                column: "WorkerId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerEvents_Workers_WorkerId",
                table: "WorkerEvents",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "WorkerId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkerEvents_Workers_WorkerId",
                table: "WorkerEvents");

            migrationBuilder.DropIndex(
                name: "IX_WorkerEvents_WorkerId",
                table: "WorkerEvents");

            migrationBuilder.AddColumn<string>(
                name: "WorkerEntityWorkerId",
                table: "WorkerEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerEvents_WorkerEntityWorkerId",
                table: "WorkerEvents",
                column: "WorkerEntityWorkerId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerEvents_Workers_WorkerEntityWorkerId",
                table: "WorkerEvents",
                column: "WorkerEntityWorkerId",
                principalTable: "Workers",
                principalColumn: "WorkerId");
        }
    }
}
