using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engine.Migrations
{
    /// <inheritdoc />
    public partial class WatchdogEventCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WatchdogEventCount",
                table: "Workers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WatchdogEventCount",
                table: "Workers");
        }
    }
}
