using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engine.Migrations
{
    /// <inheritdoc />
    public partial class watchdogSettingsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "ImgWatchdogInterval",
                table: "Workers",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 5, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT",
                oldDefaultValue: new TimeSpan(0, 0, 0, 2, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "ImgWatchdogInterval",
                table: "Workers",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 2, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT",
                oldDefaultValue: new TimeSpan(0, 0, 0, 5, 0));
        }
    }
}
