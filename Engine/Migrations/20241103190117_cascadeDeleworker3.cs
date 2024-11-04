using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engine.Migrations
{
    /// <inheritdoc />
    public partial class cascadeDeleworker3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkerEventLog_WorkerEvents_WorkerEventEventId",
                table: "WorkerEventLog");

            migrationBuilder.DropIndex(
                name: "IX_WorkerEventLog_WorkerEventEventId",
                table: "WorkerEventLog");

            migrationBuilder.DropColumn(
                name: "WorkerEventEventId",
                table: "WorkerEventLog");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerEventLog_EventId",
                table: "WorkerEventLog",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerEventLog_WorkerEvents_EventId",
                table: "WorkerEventLog",
                column: "EventId",
                principalTable: "WorkerEvents",
                principalColumn: "EventId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkerEventLog_WorkerEvents_EventId",
                table: "WorkerEventLog");

            migrationBuilder.DropIndex(
                name: "IX_WorkerEventLog_EventId",
                table: "WorkerEventLog");

            migrationBuilder.AddColumn<int>(
                name: "WorkerEventEventId",
                table: "WorkerEventLog",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerEventLog_WorkerEventEventId",
                table: "WorkerEventLog",
                column: "WorkerEventEventId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerEventLog_WorkerEvents_WorkerEventEventId",
                table: "WorkerEventLog",
                column: "WorkerEventEventId",
                principalTable: "WorkerEvents",
                principalColumn: "EventId");
        }
    }
}
