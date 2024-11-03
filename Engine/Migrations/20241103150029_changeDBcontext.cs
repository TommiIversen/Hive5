using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engine.Migrations
{
    /// <inheritdoc />
    public partial class changeDBcontext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkerEvent_Workers_WorkerEntityWorkerId",
                table: "WorkerEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkerEventLog_WorkerEvent_WorkerEventEventId",
                table: "WorkerEventLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkerEvent",
                table: "WorkerEvent");

            migrationBuilder.RenameTable(
                name: "WorkerEvent",
                newName: "WorkerEvents");

            migrationBuilder.RenameIndex(
                name: "IX_WorkerEvent_WorkerEntityWorkerId",
                table: "WorkerEvents",
                newName: "IX_WorkerEvents_WorkerEntityWorkerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkerEvents",
                table: "WorkerEvents",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerEventLog_WorkerEvents_WorkerEventEventId",
                table: "WorkerEventLog",
                column: "WorkerEventEventId",
                principalTable: "WorkerEvents",
                principalColumn: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerEvents_Workers_WorkerEntityWorkerId",
                table: "WorkerEvents",
                column: "WorkerEntityWorkerId",
                principalTable: "Workers",
                principalColumn: "WorkerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkerEventLog_WorkerEvents_WorkerEventEventId",
                table: "WorkerEventLog");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkerEvents_Workers_WorkerEntityWorkerId",
                table: "WorkerEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkerEvents",
                table: "WorkerEvents");

            migrationBuilder.RenameTable(
                name: "WorkerEvents",
                newName: "WorkerEvent");

            migrationBuilder.RenameIndex(
                name: "IX_WorkerEvents_WorkerEntityWorkerId",
                table: "WorkerEvent",
                newName: "IX_WorkerEvent_WorkerEntityWorkerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkerEvent",
                table: "WorkerEvent",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerEvent_Workers_WorkerEntityWorkerId",
                table: "WorkerEvent",
                column: "WorkerEntityWorkerId",
                principalTable: "Workers",
                principalColumn: "WorkerId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerEventLog_WorkerEvent_WorkerEventEventId",
                table: "WorkerEventLog",
                column: "WorkerEventEventId",
                principalTable: "WorkerEvent",
                principalColumn: "EventId");
        }
    }
}
