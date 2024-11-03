using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engine.Migrations
{
    /// <inheritdoc />
    public partial class addEventsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkerEvent",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkerId = table.Column<string>(type: "TEXT", nullable: false),
                    EventTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    WorkerEntityWorkerId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerEvent", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_WorkerEvent_Workers_WorkerEntityWorkerId",
                        column: x => x.WorkerEntityWorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId");
                });

            migrationBuilder.CreateTable(
                name: "WorkerEventLog",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    LogTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LogLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    WorkerEventEventId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerEventLog", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_WorkerEventLog_WorkerEvent_WorkerEventEventId",
                        column: x => x.WorkerEventEventId,
                        principalTable: "WorkerEvent",
                        principalColumn: "EventId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerEvent_WorkerEntityWorkerId",
                table: "WorkerEvent",
                column: "WorkerEntityWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerEventLog_WorkerEventEventId",
                table: "WorkerEventLog",
                column: "WorkerEventEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerEventLog");

            migrationBuilder.DropTable(
                name: "WorkerEvent");
        }
    }
}
