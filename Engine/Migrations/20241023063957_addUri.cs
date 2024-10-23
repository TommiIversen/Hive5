using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engine.Migrations
{
    /// <inheritdoc />
    public partial class addUri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HubUrlEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HubUrl = table.Column<string>(type: "TEXT", nullable: false),
                    ApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    EngineEntitiesEngineId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HubUrlEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HubUrlEntity_EngineEntities_EngineEntitiesEngineId",
                        column: x => x.EngineEntitiesEngineId,
                        principalTable: "EngineEntities",
                        principalColumn: "EngineId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HubUrlEntity_EngineEntitiesEngineId",
                table: "HubUrlEntity",
                column: "EngineEntitiesEngineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HubUrlEntity");
        }
    }
}
