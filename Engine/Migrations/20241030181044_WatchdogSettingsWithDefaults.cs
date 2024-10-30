using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engine.Migrations
{
    /// <inheritdoc />
    public partial class WatchdogSettingsWithDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ImgWatchdogEnabled",
                table: "Workers",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ImgWatchdogGraceTime",
                table: "Workers",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 10, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ImgWatchdogInterval",
                table: "Workers",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 2, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImgWatchdogEnabled",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "ImgWatchdogGraceTime",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "ImgWatchdogInterval",
                table: "Workers");
        }
    }
}
